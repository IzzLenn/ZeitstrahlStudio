using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Shared;

namespace ZeitstrahlStudio.Infrastructure;

public sealed partial class ProjectArchiveService
{
    /// <inheritdoc />
    public async partial Task<OperationResult<ProjectImportResult>> ImportAsync(
        string archivePath,
        string targetWorkingDirectory,
        IProgress<FileOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        string? temporaryDirectory = null;
        try
        {
            var sourcePath = ValidateArchiveFilePath(archivePath);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Das ausgewählte Projektarchiv wurde nicht gefunden.", sourcePath);
            }

            var targetDirectory = Path.GetFullPath(targetWorkingDirectory);
            if (Directory.Exists(targetDirectory) || File.Exists(targetDirectory))
            {
                throw new IOException("Der Zielarbeitsordner ist bereits vorhanden und wird nicht überschrieben.");
            }

            var parentDirectory = Path.GetDirectoryName(targetDirectory)
                ?? throw new ArgumentException("Der Zielarbeitsordner besitzt kein gültiges Elternverzeichnis.", nameof(targetWorkingDirectory));
            Directory.CreateDirectory(parentDirectory);
            temporaryDirectory = targetDirectory + $".importing-{Guid.NewGuid():N}";

            await using var archiveStream = new FileStream(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                128 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: false);
            var manifest = await ReadManifestAsync(archive, cancellationToken).ConfigureAwait(false);
            var validated = ValidateArchiveStructure(archive, manifest);
            EnsureDiskSpace(targetDirectory, validated.TotalBytes);

            Directory.CreateDirectory(temporaryDirectory);
            var successfulFiles = 0;
            for (var index = 0; index < manifest.Files.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var file = manifest.Files[index];
                var destinationPath = ArchivePathValidator.ResolveUnderRoot(temporaryDirectory, file.Path);
                var destinationParent = Path.GetDirectoryName(destinationPath)
                    ?? throw new InvalidDataException($"Der Archivpfad '{file.Path}' besitzt kein Zielverzeichnis.");
                Directory.CreateDirectory(destinationParent);

                await using var source = validated.Entries[file.Path].Open();
                await using var destination = new FileStream(
                    destinationPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    128 * 1024,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                var extracted = await CopyAndHashAsync(source, destination, cancellationToken).ConfigureAwait(false);
                await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
                if (extracted.Length != file.Length ||
                    !string.Equals(extracted.Sha256, file.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        $"Die Prüfsumme der Archivdatei '{file.Path}' stimmt nicht mit dem Manifest überein.");
                }

                successfulFiles++;
                progress?.Report(new FileOperationProgress(
                    file.Path,
                    index + 1,
                    manifest.Files.Count,
                    successfulFiles,
                    0));
            }

            EnsureProjectDirectories(temporaryDirectory);
            var databasePath = Path.Combine(temporaryDirectory, "project.db");
            var project = await repository.LoadAsync(databasePath, cancellationToken).ConfigureAwait(false);
            if (project.Id != manifest.ProjectId ||
                !string.Equals(project.Name, manifest.ProjectName, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "Projekt-ID oder Projektname der Datenbank stimmen nicht mit dem Manifest überein.");
            }

            SqliteConnection.ClearAllPools();
            Directory.Move(temporaryDirectory, targetDirectory);
            temporaryDirectory = null;
            var workspace = new ProjectWorkspace(project, targetDirectory, sourcePath, HasUnsavedChanges: false);
            return OperationResult<ProjectImportResult>.Success(
                new ProjectImportResult(workspace, successfulFiles, Array.Empty<string>()));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            return OperationResult<ProjectImportResult>.Failure(ToApplicationError("importiert", exception));
        }
        finally
        {
            if (temporaryDirectory is not null && Directory.Exists(temporaryDirectory))
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(temporaryDirectory, recursive: true);
            }
        }
    }

    private static void EnsureProjectDirectories(string workingDirectory)
    {
        foreach (var directoryName in ProjectDirectories)
        {
            Directory.CreateDirectory(Path.Combine(workingDirectory, directoryName));
        }
    }
}
