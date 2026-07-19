using System.Security.Cryptography;
using ZeitstrahlStudio.Infrastructure;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class LocalAttachmentImportServiceTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        "ZeitstrahlStudio.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ImportAsync_CopiesSameNamedFilesToUniqueCheckedPaths()
    {
        var workspace = CreateDirectory("workspace");
        Directory.CreateDirectory(Path.Combine(workspace, "attachments"));
        var firstSource = CreateSource("source-a", "gleich.pdf", "Erster Inhalt");
        var secondSource = CreateSource("source-b", "gleich.pdf", "Zweiter Inhalt");
        var progressReports = new List<ZeitstrahlStudio.Application.FileOperationProgress>();
        var progress = new InlineProgress<ZeitstrahlStudio.Application.FileOperationProgress>(
            progressReports.Add);
        var eventId = Guid.NewGuid();
        var service = new LocalAttachmentImportService();

        var results = await service.ImportAsync(
            eventId,
            [firstSource, secondSource],
            workspace,
            progress,
            CancellationToken.None);

        Assert.All(results, result => Assert.True(result.IsSuccess));
        var attachments = results.Select(result => result.Value!).ToArray();
        Assert.NotEqual(attachments[0].ProjectRelativePath, attachments[1].ProjectRelativePath);
        Assert.All(attachments, attachment =>
        {
            Assert.Equal("gleich.pdf", attachment.OriginalFileName);
            Assert.StartsWith($"attachments/{eventId:N}/", attachment.ProjectRelativePath);
            var target = Path.Combine(
                workspace,
                attachment.ProjectRelativePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(target));
            Assert.Equal(
                attachment.Sha256,
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(target))).ToLowerInvariant());
        });
        Assert.Equal(2, progressReports.Count);
        Assert.Equal(2, progressReports[^1].SuccessfulItems);
        Assert.Equal(0, progressReports[^1].FailedItems);
    }

    [Fact]
    public async Task ImportAsync_ContinuesAfterMissingFile()
    {
        var workspace = CreateDirectory("workspace");
        var existing = CreateSource("source", "bild.png", "Bilddaten");
        var missing = Path.Combine(root, "fehlt.pdf");
        var service = new LocalAttachmentImportService();

        var results = await service.ImportAsync(
            Guid.NewGuid(),
            [missing, existing],
            workspace,
            progress: null,
            CancellationToken.None);

        Assert.False(results[0].IsSuccess);
        Assert.Equal("AttachmentImportFailed", results[0].Error!.Code);
        Assert.True(results[1].IsSuccess);
    }

    [Fact]
    public async Task ImportAsync_CancellationLeavesNoPartialFile()
    {
        var workspace = CreateDirectory("workspace");
        var source = CreateSource("source", "gross.bin", new string('x', 2_000_000));
        var service = new LocalAttachmentImportService();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ImportAsync(
                Guid.NewGuid(),
                [source],
                workspace,
                progress: null,
                cancellation.Token));

        var attachmentsDirectory = Path.Combine(workspace, "attachments");
        Assert.False(Directory.Exists(attachmentsDirectory) &&
            Directory.EnumerateFiles(attachmentsDirectory, "*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task ImportedCopy_RemainsAfterSourceDeletion()
    {
        var workspace = CreateDirectory("workspace");
        var source = CreateSource("source", "dokument.docx", "Lokale Kopie");
        var service = new LocalAttachmentImportService();
        var result = Assert.Single(await service.ImportAsync(
            Guid.NewGuid(),
            [source],
            workspace,
            progress: null,
            CancellationToken.None));

        File.Delete(source);

        var attachment = Assert.IsType<ZeitstrahlStudio.Domain.Attachment>(result.Value);
        var copiedPath = Path.Combine(
            workspace,
            attachment.ProjectRelativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.Equal("Lokale Kopie", await File.ReadAllTextAsync(copiedPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private string CreateDirectory(string name)
    {
        var path = Path.Combine(root, name);
        Directory.CreateDirectory(path);
        return path;
    }

    private string CreateSource(string directoryName, string fileName, string content)
    {
        var directory = CreateDirectory(directoryName);
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
