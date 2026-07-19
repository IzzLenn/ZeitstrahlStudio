using System.Security.Cryptography;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Infrastructure;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class LocalAttachmentFileServiceTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "ZeitstrahlStudio.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task GetValidatedLocalPathAsync_AcceptsUnchangedProjectCopy()
    {
        var fixture = await CreateFixtureAsync("unverändert"u8.ToArray());
        var service = new LocalAttachmentFileService();

        var path = await service.GetValidatedLocalPathAsync(
            fixture.Workspace,
            fixture.Attachment,
            CancellationToken.None);

        Assert.Equal(Path.GetFullPath(fixture.Path), path);
    }

    [Fact]
    public async Task GetValidatedLocalPathAsync_RejectsSameLengthChecksumChange()
    {
        var fixture = await CreateFixtureAsync("original"u8.ToArray());
        await File.WriteAllBytesAsync(fixture.Path, "manipul8"u8.ToArray());
        var service = new LocalAttachmentFileService();

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            service.GetValidatedLocalPathAsync(
                fixture.Workspace,
                fixture.Attachment,
                CancellationToken.None));

        Assert.Contains("Prüfsumme", exception.Message);
    }

    [Fact]
    public async Task GetValidatedLocalPathAsync_PropagatesCancellation()
    {
        var fixture = await CreateFixtureAsync(new byte[1024 * 1024]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = new LocalAttachmentFileService();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.GetValidatedLocalPathAsync(
                fixture.Workspace,
                fixture.Attachment,
                cancellation.Token));
    }

    private async Task<AttachmentFixture> CreateFixtureAsync(byte[] content)
    {
        var relativePath = $"attachments/{Guid.NewGuid():N}/bild.png";
        var path = Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content);
        var attachment = new Attachment(
            Guid.NewGuid(),
            "bild.png",
            "image/png",
            content.Length,
            Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant(),
            null,
            new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero),
            relativePath);
        var project = TimelineProject.Create(
            Guid.NewGuid(),
            "Dateiprüfung",
            new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero));
        return new AttachmentFixture(
            new ProjectWorkspace(project, directory, null, true),
            attachment,
            path);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed record AttachmentFixture(
        ProjectWorkspace Workspace,
        Attachment Attachment,
        string Path);
}
