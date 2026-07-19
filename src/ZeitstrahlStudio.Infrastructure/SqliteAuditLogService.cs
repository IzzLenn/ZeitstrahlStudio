using System.Globalization;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Infrastructure;

/// <summary>Persistiert das projektbezogene Änderungsprotokoll in der Arbeitsdatenbank.</summary>
public sealed class SqliteAuditLogService : IAuditLogService
{
    /// <inheritdoc />
    public async Task WriteAsync(
        ProjectWorkspace workspace,
        AuditEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        Validate(entry);
        var databasePath = GetDatabasePath(workspace);
        await using var connection = await SqliteConnectionFactory
            .OpenAsync(databasePath, cancellationToken)
            .ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO AuditLog (
                Id, ProjectId, TimestampUtc, Operation, EntityType,
                EntityId, Description, Succeeded, TechnicalDetails)
            VALUES (
                $id, $projectId, $timestampUtc, $operation, $entityType,
                $entityId, $description, $succeeded, $technicalDetails);
            """;
        command.Parameters.AddWithValue("$id", entry.Id.ToString("D"));
        command.Parameters.AddWithValue("$projectId", workspace.Project.Id.ToString("D"));
        command.Parameters.AddWithValue(
            "$timestampUtc",
            entry.TimestampUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$operation", entry.Operation.Trim());
        command.Parameters.AddWithValue("$entityType", entry.EntityType.Trim());
        command.Parameters.AddWithValue(
            "$entityId",
            entry.EntityId?.ToString("D") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$description", entry.Description.Trim());
        command.Parameters.AddWithValue("$succeeded", entry.Succeeded ? 1 : 0);
        command.Parameters.AddWithValue(
            "$technicalDetails",
            string.IsNullOrWhiteSpace(entry.TechnicalDetails)
                ? DBNull.Value
                : entry.TechnicalDetails.Trim());
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AuditEntry>> ReadAsync(
        ProjectWorkspace workspace,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var databasePath = GetDatabasePath(workspace);
        await using var connection = await SqliteConnectionFactory
            .OpenAsync(databasePath, cancellationToken)
            .ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                Id, TimestampUtc, Operation, EntityType, EntityId,
                Description, Succeeded, TechnicalDetails
            FROM AuditLog
            WHERE ProjectId = $projectId
            ORDER BY TimestampUtc DESC, Id DESC;
            """;
        command.Parameters.AddWithValue("$projectId", workspace.Project.Id.ToString("D"));
        var entries = new List<AuditEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            entries.Add(new AuditEntry(
                Guid.Parse(reader.GetString(0)),
                DateTimeOffset.Parse(
                    reader.GetString(1),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind),
                reader.GetString(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : Guid.Parse(reader.GetString(4)),
                reader.GetString(5),
                reader.GetInt64(6) != 0,
                reader.IsDBNull(7) ? null : reader.GetString(7)));
        }

        return entries;
    }

    private static string GetDatabasePath(ProjectWorkspace workspace)
    {
        var workingDirectory = Path.GetFullPath(workspace.WorkingDirectory);
        var databasePath = Path.GetFullPath(Path.Combine(workingDirectory, "project.db"));
        if (!ArchivePathValidator.IsUnderRoot(workingDirectory, databasePath) ||
            !File.Exists(databasePath))
        {
            throw new InvalidDataException("Die Projektdatenbank für das Änderungsprotokoll wurde nicht gefunden.");
        }

        return databasePath;
    }

    private static void Validate(AuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (entry.Id == Guid.Empty)
        {
            throw new DomainValidationException("Ein Audit-Eintrag benötigt eine gültige ID.", nameof(entry));
        }

        if (entry.TimestampUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainValidationException(
                "Der Audit-Zeitpunkt muss in UTC gespeichert werden.",
                nameof(entry));
        }

        if (string.IsNullOrWhiteSpace(entry.Operation) ||
            string.IsNullOrWhiteSpace(entry.EntityType) ||
            string.IsNullOrWhiteSpace(entry.Description))
        {
            throw new DomainValidationException(
                "Audit-Operation, Entitätstyp und Beschreibung dürfen nicht leer sein.",
                nameof(entry));
        }
    }
}
