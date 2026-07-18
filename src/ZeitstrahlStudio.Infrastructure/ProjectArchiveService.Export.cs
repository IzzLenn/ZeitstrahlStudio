using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Application;

namespace ZeitstrahlStudio.Infrastructure;

public sealed partial class ProjectArchiveService
{
    private static readonly string[] ProjectDirectories =
    [
        "attachments",
        "thumbnails",
        "extracted-text",
        "logs",
        "metadata",
    ];

    /// <inheritdoc />
    public async partial Task ExportAsync(
        string workingDirectory,
        string targetArchivePath,
        IProgress<FileOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var workspaceRoot = Path.GetFullPath(workingDirectory);
        var targetPath = ValidateArchiveFilePath(targetArchivePath);
        if (!Directory.Exists(workspaceRoot))
        {
            throw new DirectoryNotFoundException("Der lokale Projektarbeitsordner wurde nicht gefunden.");
        }

        if (ArchivePathValidator.IsUnderRoot(workspaceRoot, targetPath))
        {
            throw new InvalidOperationException("Das Projektarchiv darf nicht im internen Arbeitsordner gespeichert werden.");
        }

        var databasePath = Path.Combine(workspaceRoot, "project.db");
        if (!File.Exists(databasePath))
        {
            throw new InvalidDataException("Der Arbeitsordner enthält keine Projektdatenbank.");
        }

        var project = await repository.LoadAsync(databasePath, cancellationToken).ConfigureAwait(false);
        await CheckpointDatabaseAsync(databasePath, cancellationToken).ConfigureAwait(false);
        var sourceFiles = CollectSourceFiles(workspaceRoot);
        var totalSourceBytes = sourceFiles.Aggregate(0L, (total, file) => checked(total + file.Length));

        var targetDirectory = Path.GetDirectoryName(targetPath)
            ?? throw new ArgumentException("Der Zielpfad besitzt kein gültiges Verzeichnis.", nameof(targetArchivePath));
        Directory.CreateDirectory(targetDirectory);
        EnsureDiskSpace(targetPath, totalSourceBytes);

        var temporaryPath = Path.Combine(
            targetDirectory,
            $".{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            var exportedAtUtc = timeProvider.GetUtcNow();
            var manifestFiles = await WriteArchiveAsync(
                temporaryPath,
                sourceFiles,
                project.Id,
                project.Name,
                project.CreatedAtUtc,
                exportedAtUtc,
                progress,
                cancellationToken).ConfigureAwait(false);

            await VerifyArchiveAsync(temporaryPath, manifestFiles, cancellationToken).ConfigureAwait(false);
            ReplaceAtomically(temporaryPath, targetPath);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static async Task<IReadOnlyList<ProjectArchiveFileEntry>> WriteArchiveAsync(
        string temporaryPath,
        IReadOnlyList<SourceFile> sourceFiles,
        Guid projectId,
        string projectName,
        DateTimeOffset createdAtUtc,
        DateTimeOffset exportedAtUtc,
        IProgress<FileOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var manifestFiles = new List<ProjectArchiveFileEntry>(sourceFiles.Count);
        await using var output = new FileStream(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            128 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (var index = 0; index < sourceFiles.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sourceFile = sourceFiles[index];
                var entry = archive.CreateEntry(sourceFile.ArchivePath, SelectCompression(sourceFile.ArchivePath));
                entry.LastWriteTime = ClampZipTimestamp(File.GetLastWriteTimeUtc(sourceFile.FullPath));

                await using var source = new FileStream(
                    sourceFile.FullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    128 * 1024,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                await using var destination = entry.Open();
                var copied = await CopyAndHashAsync(source, destination, cancellationToken).ConfigureAwait(false);
                if (copied.Length != sourceFile.Length)
                {
                    throw new IOException($"Die Datei '{sourceFile.ArchivePath}' wurde während des Exports verändert.");
                }

                manifestFiles.Add(new ProjectArchiveFileEntry
                {
                    Path = sourceFile.ArchivePath,
                    Length = copied.Length,
                    Sha256 = copied.Sha256,
                });
                progress?.Report(new FileOperationProgress(
                    sourceFile.ArchivePath,
                    index + 1,
                    sourceFiles.Count,
                    index + 1,
                    0));
            }

            var manifest = new ProjectArchiveManifest
            {
                Format = FormatIdentifier,
                FormatVersion = CurrentFormatVersion,
                MinimumReaderVersion = 1,
                ApplicationVersion = typeof(ProjectArchiveService).Assembly.GetName().Version?.ToString(3) ?? "1.0.0",
                ProjectId = projectId,
                ProjectName = projectName,
                CreatedAtUtc = createdAtUtc,
                ExportedAtUtc = exportedAtUtc,
                Files = manifestFiles.OrderBy(file => file.Path, StringComparer.Ordinal).ToArray(),
            };
            var manifestEntry = archive.CreateEntry("manifest.json", CompressionLevel.Optimal);
            manifestEntry.LastWriteTime = ClampZipTimestamp(exportedAtUtc.UtcDateTime);
            await using var manifestStream = manifestEntry.Open();
            await JsonSerializer.SerializeAsync(
                manifestStream,
                manifest,
                JsonOptions,
                cancellationToken).ConfigureAwait(false);
        }

        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
        output.Flush(flushToDisk: true);
        return manifestFiles;
    }

    private static IReadOnlyList<SourceFile> CollectSourceFiles(string workspaceRoot)
    {
        if ((File.GetAttributes(workspaceRoot) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("Der Projektarbeitsordner darf keine Dateisystemverknüpfung sein.");
        }

        var result = new List<SourceFile>();
        AddSourceFile(result, workspaceRoot, Path.Combine(workspaceRoot, "project.db"));

        foreach (var directoryName in ProjectDirectories)
        {
            var directory = Path.Combine(workspaceRoot, directoryName);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (var file in EnumerateFilesWithoutReparsePoints(directory))
            {
                AddSourceFile(result, workspaceRoot, file);
            }
        }

        if (result.Count > MaximumEntryCount)
        {
            throw new InvalidDataException("Das Projekt enthält zu viele Dateien für ein einzelnes Archiv.");
        }

        return result.OrderBy(file => file.ArchivePath, StringComparer.Ordinal).ToArray();
    }

    private static IEnumerable<string> EnumerateFilesWithoutReparsePoints(string rootDirectory)
    {
        var pending = new Stack<string>();
        pending.Push(rootDirectory);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException(
                    $"Der Projektordner enthält die nicht erlaubte Dateisystemverknüpfung '{directory}'.");
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
            {
                if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                {
                    throw new InvalidDataException(
                        $"Der Projektordner enthält die nicht erlaubte Dateisystemverknüpfung '{file}'.");
                }

                yield return file;
            }

            foreach (var child in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
            {
                if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) != 0)
                {
                    throw new InvalidDataException(
                        $"Der Projektordner enthält die nicht erlaubte Dateisystemverknüpfung '{child}'.");
                }

                pending.Push(child);
            }
        }
    }

    private static void AddSourceFile(List<SourceFile> result, string workspaceRoot, string fullPath)
    {
        var attributes = File.GetAttributes(fullPath);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException($"Die Datei '{fullPath}' ist eine nicht erlaubte Dateisystemverknüpfung.");
        }

        var archivePath = Path.GetRelativePath(workspaceRoot, fullPath).Replace('\\', '/');
        archivePath = ArchivePathValidator.ValidateAndNormalize(archivePath);
        var length = new FileInfo(fullPath).Length;
        if (length > MaximumSingleFileBytes)
        {
            throw new InvalidDataException($"Die Datei '{archivePath}' ist zu groß für das Projektarchiv.");
        }

        result.Add(new SourceFile(fullPath, archivePath, length));
    }

    private static async Task VerifyArchiveAsync(
        string archivePath,
        IReadOnlyList<ProjectArchiveFileEntry> expectedFiles,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            archivePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            128 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var manifest = await ReadManifestAsync(archive, cancellationToken).ConfigureAwait(false);
        var validated = ValidateArchiveStructure(archive, manifest);
        if (validated.Entries.Count != expectedFiles.Count)
        {
            throw new InvalidDataException("Das neu erstellte Projektarchiv ist unvollständig.");
        }

        foreach (var file in manifest.Files)
        {
            await using var entryStream = validated.Entries[file.Path].Open();
            var actual = await CopyAndHashAsync(entryStream, null, cancellationToken).ConfigureAwait(false);
            if (actual.Length != file.Length ||
                !string.Equals(actual.Sha256, file.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Die Prüfung der exportierten Datei '{file.Path}' ist fehlgeschlagen.");
            }
        }
    }

    private static async Task CheckpointDatabaseAsync(string databasePath, CancellationToken cancellationToken)
    {
        await using var connection = await SqliteConnectionFactory.OpenAsync(databasePath, cancellationToken)
            .ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        SqliteConnection.ClearPool(connection);
    }

    private static string ValidateArchiveFilePath(string archivePath)
    {
        if (string.IsNullOrWhiteSpace(archivePath))
        {
            throw new ArgumentException("Der Projektarchivpfad darf nicht leer sein.", nameof(archivePath));
        }

        var fullPath = Path.GetFullPath(archivePath);
        if (!string.Equals(Path.GetExtension(fullPath), ".zeitprojekt", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Projektarchive müssen die Dateiendung '.zeitprojekt' besitzen.", nameof(archivePath));
        }

        return fullPath;
    }

    private static CompressionLevel SelectCompression(string archivePath)
    {
        var extension = Path.GetExtension(archivePath);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)
            ? CompressionLevel.NoCompression
            : CompressionLevel.Optimal;
    }

    private static DateTimeOffset ClampZipTimestamp(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        var minimum = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var maximum = new DateTime(2107, 12, 31, 23, 59, 58, DateTimeKind.Utc);
        return new DateTimeOffset(utc < minimum ? minimum : utc > maximum ? maximum : utc);
    }

    private static void ReplaceAtomically(string temporaryPath, string targetPath)
    {
        if (!File.Exists(targetPath))
        {
            File.Move(temporaryPath, targetPath);
            return;
        }

        var backupPath = targetPath + $".{Guid.NewGuid():N}.previous";
        File.Replace(temporaryPath, targetPath, backupPath, ignoreMetadataErrors: true);
        try
        {
            File.Delete(backupPath);
        }
        catch (IOException)
        {
            // Der gültige vorherige Stand bleibt als lokale, wiederherstellbare Datei erhalten.
        }
        catch (UnauthorizedAccessException)
        {
            // Der eigentliche atomare Austausch war erfolgreich; die Sicherungsdatei bleibt erhalten.
        }
    }

    private sealed record SourceFile(string FullPath, string ArchivePath, long Length);
}
