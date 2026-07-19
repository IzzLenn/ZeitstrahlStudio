using UglyToad.PdfPig;
using System.Security.Cryptography;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.DocumentProcessing;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Export;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class SkiaPdfExportServiceTests : IDisposable
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);
    private readonly string temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        "ZeitstrahlStudio.Tests",
        Guid.NewGuid().ToString("N"));

    public SkiaPdfExportServiceTests() => Directory.CreateDirectory(temporaryDirectory);

    [Fact]
    public async Task ExportAsync_CreatesReadableVectorPdfWithExpectedPagesAndText()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Chronik ÄÖÜ", Timestamp);
        for (var index = 0; index < 18; index++)
        {
            var timelineEvent = TimelineEvent.Create(
                Guid.NewGuid(),
                $"Prüfereignis {index + 1}",
                EventDate.Exact(new DateOnly(2026, 1, 1).AddDays(index)),
                Timestamp);
            timelineEvent.UpdateContent(
                timelineEvent.Title,
                "Kurzer Infotext",
                "Vollständige Beschreibung mit lokalem Inhalt.",
                "Eigene Quelle",
                index == 0 ? "Nur bei aktiviertem Notizexport" : null,
                Timestamp);
            if (index == 0)
            {
                timelineEvent.SetDeadline(
                    new Deadline(
                        Guid.NewGuid(),
                        new DateOnly(2026, 2, 1),
                        new TimeOnly(9, 30),
                        "Abgabe",
                        DeadlineStatus.Open),
                    Timestamp);
            }

            project.AddEvent(timelineEvent, Timestamp);
        }

        var workspace = CreateWorkspace(project);
        var service = new SkiaPdfExportService(
            new PdfExportPlanner(),
            new UnexpectedAttachmentFileService(),
            new UnexpectedPdfPreviewService());
        var options = DefaultOptions() with { IncludeNotes = true };
        var preview = await service.CreatePreviewAsync(workspace, options, CancellationToken.None);
        var targetPath = Path.Combine(temporaryDirectory, "zeitstrahl.pdf");

        await service.ExportAsync(workspace, options, targetPath, CancellationToken.None);

        Assert.True(File.Exists(targetPath));
        Assert.True(new FileInfo(targetPath).Length > 1_000);
        using var document = PdfDocument.Open(targetPath);
        Assert.Equal(preview.PageCount, document.NumberOfPages);
        var text = string.Join('\n', document.GetPages().Select(page => page.Text));
        Assert.Contains("Chronik ÄÖÜ", text, StringComparison.Ordinal);
        Assert.Contains("Prüfereignis 1", text, StringComparison.Ordinal);
        Assert.Contains("Frist: 01.02.2026 09:30", text, StringComparison.Ordinal);
        Assert.Contains("Nur bei aktiviertem Notizexport", text, StringComparison.Ordinal);

        var renderedPage = await new PdfiumPdfPreviewService().RenderPageAsync(
            targetPath,
            1,
            0.5,
            CancellationToken.None);
        Assert.Equal(preview.PageCount, renderedPage.PageCount);
        Assert.True(renderedPage.PngData.Length > 1_000);
    }

    [Fact]
    public async Task ExportAsync_OverwritesTargetAtomicallyAndHonorsCancellation()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Atomarer Export", Timestamp);
        project.AddEvent(
            TimelineEvent.Create(
                Guid.NewGuid(),
                "Ein Ereignis",
                EventDate.Year(2026),
                Timestamp),
            Timestamp);
        var workspace = CreateWorkspace(project);
        var service = new SkiaPdfExportService(
            new PdfExportPlanner(),
            new UnexpectedAttachmentFileService(),
            new UnexpectedPdfPreviewService());
        var targetPath = Path.Combine(temporaryDirectory, "vorhanden.pdf");
        await File.WriteAllTextAsync(targetPath, "alt");

        await service.ExportAsync(workspace, DefaultOptions(), targetPath, CancellationToken.None);

        Assert.Equal("%PDF", (await File.ReadAllTextAsync(targetPath))[..4]);
        Assert.Empty(Directory.EnumerateFiles(temporaryDirectory, "*.tmp"));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.ExportAsync(
            workspace,
            DefaultOptions(),
            Path.Combine(temporaryDirectory, "abgebrochen.pdf"),
            cancellation.Token));
        Assert.False(File.Exists(Path.Combine(temporaryDirectory, "abgebrochen.pdf")));

        var protectedTarget = Path.Combine(workspace.WorkingDirectory, "projektintern.pdf");
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExportAsync(
            workspace,
            DefaultOptions(),
            protectedTarget,
            CancellationToken.None));
        Assert.Contains("Projektarbeitsordners", exception.Message, StringComparison.Ordinal);
        Assert.False(File.Exists(protectedTarget));
    }

    [Fact]
    public async Task ExportAsync_LoadsValidatedPrimaryImageAndKeepsDocumentReference()
    {
        var pngData = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAFElEQVR4nGP4z8DAwMDAxMDAwMAAAAwAAf4CB0kAAAAASUVORK5CYII=");
        var imagePath = Path.Combine(temporaryDirectory, "miniatur.png");
        await File.WriteAllBytesAsync(imagePath, pngData);
        var project = TimelineProject.Create(Guid.NewGuid(), "Vorschaubild", Timestamp);
        var timelineEvent = TimelineEvent.Create(
            Guid.NewGuid(),
            "Ereignis mit Bild",
            EventDate.Exact(new DateOnly(2026, 7, 19)),
            Timestamp);
        timelineEvent.AddAttachment(
            new Attachment(
                Guid.NewGuid(),
                "miniatur.png",
                "image/png",
                pngData.Length,
                Convert.ToHexString(SHA256.HashData(pngData)).ToLowerInvariant(),
                null,
                Timestamp,
                "attachments/miniatur.png"),
            Timestamp);
        project.AddEvent(timelineEvent, Timestamp);
        var workspace = CreateWorkspace(project);
        var attachmentFiles = new FixedAttachmentFileService(imagePath);
        var service = new SkiaPdfExportService(
            new PdfExportPlanner(),
            attachmentFiles,
            new UnexpectedPdfPreviewService());
        var targetPath = Path.Combine(temporaryDirectory, "mit-miniatur.pdf");

        await service.ExportAsync(workspace, DefaultOptions(), targetPath, CancellationToken.None);

        Assert.Equal(1, attachmentFiles.ValidationCount);
        using var document = PdfDocument.Open(targetPath);
        Assert.Contains("Dokumente: miniatur.png", document.GetPage(1).Text, StringComparison.Ordinal);
        var rendered = await new PdfiumPdfPreviewService().RenderPageAsync(
            targetPath,
            1,
            0.5,
            CancellationToken.None);
        Assert.True(rendered.PngData.Length > 1_000);
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private static PdfExportOptions DefaultOptions() => new(
        PaperSize: "A4",
        Landscape: false,
        WidthMillimeters: 210,
        HeightMillimeters: 297,
        FontSize: 10,
        RangeStart: null,
        RangeEnd: null,
        IncludeOverlappingRanges: true,
        SingleLargePage: false,
        IncludeNotes: false);

    private ProjectWorkspace CreateWorkspace(TimelineProject project)
    {
        var workingDirectory = Path.Combine(temporaryDirectory, "workspace");
        Directory.CreateDirectory(workingDirectory);
        return new ProjectWorkspace(project, workingDirectory, null, false);
    }

    private sealed class UnexpectedAttachmentFileService : IAttachmentFileService
    {
        public Task<string> GetValidatedLocalPathAsync(
            ProjectWorkspace workspace,
            Attachment attachment,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Für diesen Test darf kein Anhang geladen werden.");

        public Task OpenWithDefaultApplicationAsync(
            ProjectWorkspace workspace,
            Attachment attachment,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Für diesen Test darf kein Anhang geöffnet werden.");
    }

    private sealed class UnexpectedPdfPreviewService : IPdfPreviewService
    {
        public Task<PdfPagePreview> RenderPageAsync(
            string validatedLocalPath,
            int pageNumber,
            double renderScale,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Für diesen Test darf keine PDF-Miniatur geladen werden.");
    }

    private sealed class FixedAttachmentFileService(string path) : IAttachmentFileService
    {
        public int ValidationCount { get; private set; }

        public Task<string> GetValidatedLocalPathAsync(
            ProjectWorkspace workspace,
            Attachment attachment,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ValidationCount++;
            return Task.FromResult(path);
        }

        public Task OpenWithDefaultApplicationAsync(
            ProjectWorkspace workspace,
            Attachment attachment,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Für diesen Test darf kein Anhang geöffnet werden.");
    }
}
