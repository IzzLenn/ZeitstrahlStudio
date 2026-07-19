using System.Buffers;
using System.Security.Cryptography;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Shared;

namespace ZeitstrahlStudio.Infrastructure;

/// <summary>Kopiert externe Dateien streamend und kollisionsfrei in einen Projektarbeitsordner.</summary>
public sealed class LocalAttachmentImportService : IAttachmentImportService
{
    private const int BufferSize = 128 * 1024;
    private readonly TimeProvider timeProvider;

    public LocalAttachmentImportService(TimeProvider? timeProvider = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OperationResult<Attachment>>> ImportAsync(
        Guid eventId,
        IReadOnlyCollection<string> sourcePaths,
        string workingDirectory,
        IProgress<FileOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Für den Anhangsimport ist eine gültige Ereignis-ID erforderlich.", nameof(eventId));
        }

        ArgumentNullException.ThrowIfNull(sourcePaths);
        if (sourcePaths.Count == 0)
        {
            return [];
        }

        var workspaceRoot = Path.GetFullPath(workingDirectory);
        if (!Directory.Exists(workspaceRoot))
        {
            throw new DirectoryNotFoundException("Der Projektarbeitsordner wurde nicht gefunden.");
        }

        var attachmentsRoot = ArchivePathValidator.ResolveUnderRoot(workspaceRoot, "attachments");
        Directory.CreateDirectory(attachmentsRoot);
        if ((File.GetAttributes(attachmentsRoot) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException("Der projektinterne Anhangsordner darf keine Dateisystemverknüpfung sein.");
        }

        var results = new List<OperationResult<Attachment>>(sourcePaths.Count);
        var successful = 0;
        var failed = 0;
        var completed = 0;
        foreach (var sourcePath in sourcePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OperationResult<Attachment> result;
            try
            {
                var attachment = await ImportSingleAsync(
                    eventId,
                    sourcePath,
                    workspaceRoot,
                    cancellationToken).ConfigureAwait(false);
                result = OperationResult<Attachment>.Success(attachment);
                successful++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is IOException
                    or UnauthorizedAccessException
                    or ArgumentException
                    or NotSupportedException
                    or DomainValidationException)
            {
                result = OperationResult<Attachment>.Failure(new ApplicationError(
                    "AttachmentImportFailed",
                    $"Die Datei „{GetDisplayName(sourcePath)}“ konnte nicht importiert werden.",
                    exception.Message));
                failed++;
            }

            results.Add(result);
            completed++;
            progress?.Report(new FileOperationProgress(
                GetDisplayName(sourcePath),
                completed,
                sourcePaths.Count,
                successful,
                failed));
        }

        return results;
    }

    private async Task<Attachment> ImportSingleAsync(
        Guid eventId,
        string sourcePath,
        string workspaceRoot,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Ein Quelldateipfad ist leer.", nameof(sourcePath));
        }

        var fullSourcePath = Path.GetFullPath(sourcePath);
        var sourceInfo = new FileInfo(fullSourcePath);
        if (!sourceInfo.Exists)
        {
            throw new FileNotFoundException("Die Quelldatei wurde nicht gefunden.", fullSourcePath);
        }

        var attachmentId = Guid.NewGuid();
        var extension = NormalizeExtension(sourceInfo.Extension);
        var relativePath = $"attachments/{eventId:N}/{attachmentId:N}{extension}";
        var targetPath = ArchivePathValidator.ResolveUnderRoot(workspaceRoot, relativePath);
        var targetDirectory = Path.GetDirectoryName(targetPath)
            ?? throw new InvalidDataException("Der Zielpfad des Anhangs ist ungültig.");
        Directory.CreateDirectory(targetDirectory);
        var sourceLength = sourceInfo.Length;
        var sourceLastWriteUtc = sourceInfo.LastWriteTimeUtc;
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            await using var source = new FileStream(
                fullSourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var target = new FileStream(
                targetPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long copiedLength = 0;
            while (true)
            {
                var read = await source.ReadAsync(
                    buffer.AsMemory(0, buffer.Length),
                    cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                hash.AppendData(buffer, 0, read);
                await target.WriteAsync(
                    buffer.AsMemory(0, read),
                    cancellationToken).ConfigureAwait(false);
                copiedLength += read;
            }

            await target.FlushAsync(cancellationToken).ConfigureAwait(false);
            var currentSourceInfo = new FileInfo(fullSourcePath);
            if (copiedLength != sourceLength ||
                !currentSourceInfo.Exists ||
                currentSourceInfo.Length != sourceLength ||
                currentSourceInfo.LastWriteTimeUtc != sourceLastWriteUtc)
            {
                throw new IOException("Die Quelldatei wurde während des Imports verändert.");
            }

            var sha256 = Convert.ToHexString(hash.GetHashAndReset());
            return new Attachment(
                attachmentId,
                sourceInfo.Name,
                GetMediaType(extension),
                copiedLength,
                sha256,
                fullSourcePath,
                timeProvider.GetUtcNow(),
                relativePath,
                AttachmentState.Imported);
        }
        catch
        {
            DeleteBestEffort(targetPath);
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension) ||
            extension.Length > 20 ||
            extension.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '.'))
        {
            return string.Empty;
        }

        return extension.ToLowerInvariant();
    }

    private static string GetMediaType(string extension) => extension switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".tif" or ".tiff" => "image/tiff",
        ".bmp" => "image/bmp",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _ => "application/octet-stream",
    };

    private static string GetDisplayName(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "(unbekannte Datei)";
        }

        try
        {
            return Path.GetFileName(path);
        }
        catch (ArgumentException)
        {
            return path;
        }
    }

    private static void DeleteBestEffort(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
