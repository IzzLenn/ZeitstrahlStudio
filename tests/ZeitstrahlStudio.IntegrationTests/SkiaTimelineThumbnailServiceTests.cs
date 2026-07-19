using System.Security.Cryptography;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Export;
using ZeitstrahlStudio.Infrastructure;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class SkiaTimelineThumbnailServiceTests : IDisposable
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);
    private static readonly byte[] SourcePng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "ZeitstrahlStudio.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task GetOrCreateAsync_CreatesBoundedJpegCacheAndReusesIt()
    {
        var fixture = await CreateImageFixtureAsync();
        using var log = new JsonLinesLocalLogService(Path.Combine(directory, "logs"));
        using var service = new SkiaTimelineThumbnailService(
            new LocalAttachmentFileService(),
            new UnusedPdfPreviewService(),
            log);

        var first = await service.GetOrCreateAsync(
            fixture.Workspace,
            fixture.Attachment,
            CancellationToken.None);
        var second = await service.GetOrCreateAsync(
            fixture.Workspace,
            fixture.Attachment,
            CancellationToken.None);

        Assert.NotNull(first);
        Assert.Equal(fixture.Attachment.Id, first.AttachmentId);
        Assert.InRange(first.PixelWidth, 1, 360);
        Assert.InRange(first.PixelHeight, 1, 240);
        Assert.True(first.EncodedImageData.AsSpan(0, 2).SequenceEqual(new byte[] { 0xFF, 0xD8 }));
        Assert.Equal(first.EncodedImageData, second!.EncodedImageData);
        var cachePath = Path.Combine(
            fixture.Workspace.WorkingDirectory,
            first.CacheRelativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(cachePath));
        Assert.InRange(new FileInfo(cachePath).Length, 1, 5 * 1024 * 1024);
    }

    [Fact]
    public async Task GetOrCreateAsync_PropagatesCancellationBeforeRendering()
    {
        var fixture = await CreateImageFixtureAsync();
        using var log = new JsonLinesLocalLogService(Path.Combine(directory, "logs"));
        using var service = new SkiaTimelineThumbnailService(
            new LocalAttachmentFileService(),
            new UnusedPdfPreviewService(),
            log);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.GetOrCreateAsync(fixture.Workspace, fixture.Attachment, cancellation.Token));
    }

    private async Task<ThumbnailFixture> CreateImageFixtureAsync()
    {
        var relativePath = $"attachments/{Guid.NewGuid():N}/bild.png";
        var fullPath = Path.Combine(
            directory,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, SourcePng);
        var attachment = new Attachment(
            Guid.NewGuid(),
            "bild.png",
            "image/png",
            SourcePng.Length,
            Convert.ToHexString(SHA256.HashData(SourcePng)).ToLowerInvariant(),
            null,
            Timestamp,
            relativePath);
        var timelineEvent = TimelineEvent.Create(
            Guid.NewGuid(),
            "Mit Bild",
            EventDate.Year(2026),
            Timestamp);
        timelineEvent.AddAttachment(attachment, Timestamp);
        var project = TimelineProject.Create(Guid.NewGuid(), "Miniaturtest", Timestamp);
        project.AddEvent(timelineEvent, Timestamp);
        return new ThumbnailFixture(
            new ProjectWorkspace(project, directory, null, true),
            attachment);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed record ThumbnailFixture(ProjectWorkspace Workspace, Attachment Attachment);

    private sealed class UnusedPdfPreviewService : IPdfPreviewService
    {
        public Task<PdfPagePreview> RenderPageAsync(
            string validatedLocalPath,
            int pageNumber,
            double renderScale,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Für einen Bildanhang darf kein PDF-Renderer verwendet werden.");
    }
}
