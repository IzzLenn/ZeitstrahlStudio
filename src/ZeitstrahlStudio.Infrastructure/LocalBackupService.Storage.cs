using System.Globalization;
using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Infrastructure;

public sealed partial class LocalBackupService
{
    private async Task<IReadOnlyList<BackupRecord>> ListCoreAsync(
        ProjectWorkspace workspace,
        CancellationToken cancellationToken)
    {
        var stored = await ReadRecordsAsync(workspace, cancellationToken).ConfigureAwait(false);
        var records = stored.ToDictionary(record => record.Id);
        foreach (var record in stored)
        {
            var fullPath = ResolveBackupPath(workspace.Project.Id, record);
            if (!File.Exists(fullPath))
            {
                await DeleteRecordAsync(workspace, record.Id, cancellationToken).ConfigureAwait(false);
                records.Remove(record.Id);
            }
        }

        var projectDirectory = GetProjectBackupDirectory(workspace.Project.Id);
        foreach (var fullPath in Directory.EnumerateFiles(
                     projectDirectory,
                     "*.zeitprojekt",
                     SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((File.GetAttributes(fullPath) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException(
                    "Der lokale Sicherungsordner enthält eine nicht erlaubte Dateisystemverknüpfung.");
            }

            if (!TryParseBackupFileName(fullPath, out var id, out var createdAtUtc, out var automatic))
            {
                continue;
            }

            var relativePath = workspace.Project.Id.ToString("N") + "/" + Path.GetFileName(fullPath);
            if (records.TryGetValue(id, out var existing))
            {
                if (!string.Equals(
                        existing.RelativeArchivePath,
                        relativePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        "Die Sicherungs-ID ist mit widersprüchlichen Pfaden gespeichert.");
                }

                continue;
            }

            var fileInfo = new FileInfo(fullPath);
            var record = new BackupRecord(
                id,
                createdAtUtc,
                relativePath,
                fileInfo.Length,
                await ComputeStableSha256Async(fullPath, cancellationToken).ConfigureAwait(false),
                automatic);
            await UpsertRecordAsync(workspace, record, cancellationToken).ConfigureAwait(false);
            records.Add(record.Id, record);
        }

        return records.Values
            .OrderByDescending(record => record.CreatedAtUtc)
            .ThenByDescending(record => record.Id)
            .ToArray();
    }

    private static async Task<IReadOnlyList<BackupRecord>> ReadRecordsAsync(
        ProjectWorkspace workspace,
        CancellationToken cancellationToken)
    {
        await using var connection = await SqliteConnectionFactory
            .OpenAsync(GetDatabasePath(workspace), cancellationToken)
            .ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, CreatedAtUtc, RelativeArchivePath, FileSize, Sha256, IsAutomatic
            FROM Backups
            WHERE ProjectId = $projectId
            ORDER BY CreatedAtUtc DESC, Id DESC;
            """;
        command.Parameters.AddWithValue("$projectId", workspace.Project.Id.ToString("D"));
        var records = new List<BackupRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var automaticValue = reader.GetInt64(5);
            if (automaticValue is not 0 and not 1)
            {
                throw new InvalidDataException("Ein Sicherungseintrag enthält einen ungültigen Typ.");
            }

            var record = new BackupRecord(
                Guid.Parse(reader.GetString(0)),
                DateTimeOffset.Parse(
                    reader.GetString(1),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind),
                reader.GetString(2),
                reader.GetInt64(3),
                reader.GetString(4),
                automaticValue == 1);
            ValidateRecord(record);
            records.Add(record);
        }

        return records;
    }

    private static async Task UpsertRecordAsync(
        ProjectWorkspace workspace,
        BackupRecord record,
        CancellationToken cancellationToken)
    {
        ValidateRecord(record);
        await using var connection = await SqliteConnectionFactory
            .OpenAsync(GetDatabasePath(workspace), cancellationToken)
            .ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Backups (
                Id, ProjectId, CreatedAtUtc, RelativeArchivePath,
                FileSize, Sha256, IsAutomatic)
            VALUES (
                $id, $projectId, $createdAtUtc, $relativeArchivePath,
                $fileSize, $sha256, $isAutomatic)
            ON CONFLICT(Id) DO UPDATE SET
                ProjectId = excluded.ProjectId,
                CreatedAtUtc = excluded.CreatedAtUtc,
                RelativeArchivePath = excluded.RelativeArchivePath,
                FileSize = excluded.FileSize,
                Sha256 = excluded.Sha256,
                IsAutomatic = excluded.IsAutomatic;
            """;
        command.Parameters.AddWithValue("$id", record.Id.ToString("D"));
        command.Parameters.AddWithValue("$projectId", workspace.Project.Id.ToString("D"));
        command.Parameters.AddWithValue(
            "$createdAtUtc",
            record.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$relativeArchivePath", record.RelativeArchivePath);
        command.Parameters.AddWithValue("$fileSize", record.FileSize);
        command.Parameters.AddWithValue("$sha256", record.Sha256.ToLowerInvariant());
        command.Parameters.AddWithValue("$isAutomatic", record.IsAutomatic ? 1 : 0);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task DeleteRecordAsync(
        ProjectWorkspace workspace,
        Guid backupId,
        CancellationToken cancellationToken)
    {
        await using var connection = await SqliteConnectionFactory
            .OpenAsync(GetDatabasePath(workspace), cancellationToken)
            .ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM Backups
            WHERE Id = $id AND ProjectId = $projectId;
            """;
        command.Parameters.AddWithValue("$id", backupId.ToString("D"));
        command.Parameters.AddWithValue("$projectId", workspace.Project.Id.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string GetDatabasePath(ProjectWorkspace workspace)
    {
        var workingDirectory = Path.GetFullPath(workspace.WorkingDirectory);
        var databasePath = Path.GetFullPath(Path.Combine(workingDirectory, "project.db"));
        if (!ArchivePathValidator.IsUnderRoot(workingDirectory, databasePath) ||
            !File.Exists(databasePath))
        {
            throw new InvalidDataException(
                "Die Projektdatenbank für die Sicherungsverwaltung wurde nicht gefunden.");
        }

        return databasePath;
    }

    private static bool TryParseBackupFileName(
        string fullPath,
        out Guid id,
        out DateTimeOffset createdAtUtc,
        out bool automatic)
    {
        id = Guid.Empty;
        createdAtUtc = default;
        automatic = false;
        var parts = Path.GetFileNameWithoutExtension(fullPath).Split('_');
        if (parts.Length != 3 ||
            !DateTimeOffset.TryParseExact(
                parts[0],
                BackupTimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out createdAtUtc) ||
            !Guid.TryParseExact(parts[2], "N", out id))
        {
            return false;
        }

        if (string.Equals(parts[1], "auto", StringComparison.Ordinal))
        {
            automatic = true;
            return true;
        }

        return string.Equals(parts[1], "manual", StringComparison.Ordinal);
    }
}
