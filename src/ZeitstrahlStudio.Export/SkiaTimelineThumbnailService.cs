using SkiaSharp;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Export;

/// <summary>Erzeugt kleine lokale JPEG-Vorschauen und hält sie als ableitbaren Projektcache vor.</summary>
public sealed class SkiaTimelineThumbnailService : ITimelineThumbnailService, IDisposable
{
    private const int MaximumSourceEdge = 8_000;
    private const long MaximumSourcePixels = 24_000_000;
    private const long MaximumImageSourceBytes = 50L * 1024 * 1024;
    private const int MaximumThumbnailWidth = 360;
    private const int MaximumThumbnailHeight = 240;
    private const int MaximumCacheBytes = 5 * 1024 * 1024;
    private readonly IAttachmentFileService attachmentFileService;
    private readonly IPdfPreviewService pdfPreviewService;
    private readonly ILocalLogService logService;
    private readonly SemaphoreSlim renderGate = new(2, 2);

    public SkiaTimelineThumbnailService(
        IAttachmentFileService attachmentFileService,
        IPdfPreviewService pdfPreviewService,
        ILocalLogService logService)
    {
        this.attachmentFileService = attachmentFileService ??
            throw new ArgumentNullException(nameof(attachmentFileService));
        this.pdfPreviewService = pdfPreviewService ??
            throw new ArgumentNullException(nameof(pdfPreviewService));
        this.logService = logService ?? throw new ArgumentNullException(nameof(logService));
    }

    /// <inheritdoc />
    public async Task<TimelineThumbnail?> GetOrCreateAsync(
        ProjectWorkspace workspace,
        Attachment attachment,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(attachment);
        if (!IsSupported(attachment))
        {
            return null;
        }

        EnsureAttachmentBelongsToWorkspace(workspace, attachment);
        await renderGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var (cachePath, cacheRelativePath) = GetCachePath(workspace, attachment);
            var cached = await TryReadCacheAsync(
                cachePath,
                attachment.Id,
                cacheRelativePath,
                cancellationToken).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }

            var localPath = await attachmentFileService.GetValidatedLocalPathAsync(
                workspace,
                attachment,
                cancellationToken).ConfigureAwait(false);
            var sourceData = await LoadSourceDataAsync(
                localPath,
                attachment,
                cancellationToken).ConfigureAwait(false);
            if (sourceData is null)
            {
                await WriteWarningBestEffortAsync(
                    "ThumbnailSourceTooLarge",
                    $"Für „{attachment.OriginalFileName}“ wurde keine Kartenminiatur erzeugt, weil die Quelldatei zu groß ist.",
                    technicalDetails: null).ConfigureAwait(false);
                return null;
            }

            var encoded = await Task.Run(
                () => EncodeThumbnail(sourceData, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            if (encoded is null)
            {
                await WriteWarningBestEffortAsync(
                    "ThumbnailDecodeFailed",
                    $"Für „{attachment.OriginalFileName}“ konnte keine Kartenminiatur erzeugt werden.",
                    technicalDetails: null).ConfigureAwait(false);
                return null;
            }

            await WriteCacheAtomicallyAsync(cachePath, encoded.Data, cancellationToken)
                .ConfigureAwait(false);
            return new TimelineThumbnail(
                attachment.Id,
                encoded.Width,
                encoded.Height,
                cacheRelativePath,
                encoded.Data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidDataException or
            InvalidOperationException or ArgumentException)
        {
            await WriteWarningBestEffortAsync(
                "ThumbnailFailed",
                $"Die Kartenminiatur für „{attachment.OriginalFileName}“ konnte nicht geladen werden.",
                exception.ToString()).ConfigureAwait(false);
            return null;
        }
        finally
        {
            renderGate.Release();
        }
    }

    private async Task<byte[]?> LoadSourceDataAsync(
        string localPath,
        Attachment attachment,
        CancellationToken cancellationToken)
    {
        if (attachment.MediaType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            var preview = await pdfPreviewService.RenderPageAsync(
                localPath,
                attachment.LinkedPdfPage ?? 1,
                0.5,
                cancellationToken).ConfigureAwait(false);
            return preview.PngData;
        }

        var fileInfo = new FileInfo(localPath);
        return fileInfo.Length <= MaximumImageSourceBytes
            ? await File.ReadAllBytesAsync(localPath, cancellationToken).ConfigureAwait(false)
            : null;
    }

    private static EncodedThumbnail? EncodeThumbnail(
        byte[] sourceData,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var encodedSource = SKData.CreateCopy(sourceData);
        using var codec = SKCodec.Create(encodedSource);
        if (codec is null ||
            codec.Info.Width <= 0 ||
            codec.Info.Height <= 0 ||
            codec.Info.Width > MaximumSourceEdge ||
            codec.Info.Height > MaximumSourceEdge ||
            (long)codec.Info.Width * codec.Info.Height > MaximumSourcePixels)
        {
            return null;
        }

        using var image = SKImage.FromEncodedData(encodedSource);
        if (image is null || image.Width <= 0 || image.Height <= 0)
        {
            return null;
        }

        var scale = Math.Min(
            1d,
            Math.Min(
                MaximumThumbnailWidth / (double)image.Width,
                MaximumThumbnailHeight / (double)image.Height));
        var width = Math.Max(1, (int)Math.Round(image.Width * scale));
        var height = Math.Max(1, (int)Math.Round(image.Height * scale));
        using var surface = SKSurface.Create(new SKImageInfo(
            width,
            height,
            SKColorType.Rgba8888,
            SKAlphaType.Premul));
        if (surface is null)
        {
            return null;
        }

        surface.Canvas.Clear(SKColors.White);
        surface.Canvas.DrawImage(image, new SKRect(0, 0, width, height));
        cancellationToken.ThrowIfCancellationRequested();
        using var snapshot = surface.Snapshot();
        using var encoded = snapshot.Encode(SKEncodedImageFormat.Jpeg, 82);
        var data = encoded?.ToArray();
        return data is { Length: > 0 and <= MaximumCacheBytes }
            ? new EncodedThumbnail(width, height, data)
            : null;
    }

    private static async Task<TimelineThumbnail?> TryReadCacheAsync(
        string cachePath,
        Guid attachmentId,
        string cacheRelativePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(cachePath))
        {
            return null;
        }

        try
        {
            var fileInfo = new FileInfo(cachePath);
            if ((fileInfo.Attributes & FileAttributes.ReparsePoint) != 0 ||
                fileInfo.Length is <= 0 or > MaximumCacheBytes)
            {
                throw new InvalidDataException("Die gespeicherte Kartenminiatur ist ungültig.");
            }

            var data = await File.ReadAllBytesAsync(cachePath, cancellationToken).ConfigureAwait(false);
            using var encoded = SKData.CreateCopy(data);
            using var codec = SKCodec.Create(encoded);
            if (codec is null ||
                codec.Info.Width is <= 0 or > MaximumThumbnailWidth ||
                codec.Info.Height is <= 0 or > MaximumThumbnailHeight)
            {
                throw new InvalidDataException("Die gespeicherte Kartenminiatur ist beschädigt.");
            }

            return new TimelineThumbnail(
                attachmentId,
                codec.Info.Width,
                codec.Info.Height,
                cacheRelativePath,
                data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            DeleteBestEffort(cachePath);
            return null;
        }
    }

    private static async Task WriteCacheAtomicallyAsync(
        string cachePath,
        byte[] data,
        CancellationToken cancellationToken)
    {
        var temporaryPath = cachePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporaryPath, data, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, cachePath, overwrite: true);
        }
        finally
        {
            DeleteBestEffort(temporaryPath);
        }
    }

    private static (string FullPath, string RelativePath) GetCachePath(
        ProjectWorkspace workspace,
        Attachment attachment)
    {
        var workspaceRoot = Path.GetFullPath(workspace.WorkingDirectory);
        if (!Directory.Exists(workspaceRoot) || IsReparsePoint(workspaceRoot))
        {
            throw new InvalidDataException(
                "Der Projektarbeitsordner für die Kartenminiatur ist ungültig.");
        }

        var thumbnailRoot = Path.Combine(workspaceRoot, "thumbnails");
        EnsureCacheDirectory(thumbnailRoot);
        var timelineRoot = Path.Combine(thumbnailRoot, "timeline");
        EnsureCacheDirectory(timelineRoot);
        var page = attachment.MediaType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            ? attachment.LinkedPdfPage ?? 1
            : 0;
        var fileName = $"{attachment.Id:N}_{page}_{attachment.Sha256}.jpg";
        var fullPath = Path.GetFullPath(Path.Combine(timelineRoot, fileName));
        var expectedPrefix = timelineRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Der Pfad der Kartenminiatur ist ungültig.");
        }

        return (fullPath, "thumbnails/timeline/" + fileName);
    }

    private static void EnsureCacheDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            if (IsReparsePoint(directory))
            {
                throw new InvalidDataException(
                    "Der Ordner für Kartenminiaturen darf keine Dateisystemverknüpfung sein.");
            }

            return;
        }

        Directory.CreateDirectory(directory);
        if (IsReparsePoint(directory))
        {
            throw new InvalidDataException(
                "Der Ordner für Kartenminiaturen darf keine Dateisystemverknüpfung sein.");
        }
    }

    private static void EnsureAttachmentBelongsToWorkspace(
        ProjectWorkspace workspace,
        Attachment attachment)
    {
        var stored = workspace.Project.Events
            .SelectMany(timelineEvent => timelineEvent.Attachments)
            .SingleOrDefault(candidate => candidate.Id == attachment.Id);
        if (stored is null || stored != attachment)
        {
            throw new InvalidOperationException(
                "Die Kartenminiatur kann nur für einen Anhang des geöffneten Projekts erzeugt werden.");
        }
    }

    private async Task WriteWarningBestEffortAsync(
        string eventName,
        string message,
        string? technicalDetails)
    {
        try
        {
            await logService.WriteAsync(
                new LocalLogEntry(
                    DateTimeOffset.UtcNow,
                    LocalLogLevel.Warning,
                    nameof(SkiaTimelineThumbnailService),
                    eventName,
                    message,
                    technicalDetails),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static bool IsSupported(Attachment attachment) =>
        attachment.MediaType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
        attachment.MediaType.Equals("image/png", StringComparison.OrdinalIgnoreCase) ||
        attachment.MediaType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
        attachment.MediaType.Equals("image/tiff", StringComparison.OrdinalIgnoreCase) ||
        attachment.MediaType.Equals("image/bmp", StringComparison.OrdinalIgnoreCase);

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static void DeleteBestEffort(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <inheritdoc />
    public void Dispose() => renderGate.Dispose();

    private sealed record EncodedThumbnail(int Width, int Height, byte[] Data);
}
