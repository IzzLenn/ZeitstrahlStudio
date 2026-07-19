using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Export;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed partial class StandaloneHtmlExportServiceTests : IDisposable
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);
    private readonly string temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        "ZeitstrahlStudio.Tests",
        Guid.NewGuid().ToString("N"));

    public StandaloneHtmlExportServiceTests() => Directory.CreateDirectory(temporaryDirectory);

    [Fact]
    public async Task ExportAsync_CreatesSingleOfflineSafeInteractiveSnapshot()
    {
        var pngData = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAFElEQVR4nGP4z8DAwMDAxMDAwMAAAAwAAf4CB0kAAAAASUVORK5CYII=");
        var imagePath = Path.Combine(temporaryDirectory, "vorschaubild.png");
        await File.WriteAllBytesAsync(imagePath, pngData);
        var project = TimelineProject.Create(Guid.NewGuid(), "Chronik </script><img src=x>", Timestamp);
        project.UpdateInformation(
            project.Name,
            "Lokale & sichere Übersicht",
            "Kurzinfo",
            "Vollständig offline",
            new DateOnly(2020, 1, 1),
            new DateOnly(2030, 12, 31),
            Timestamp);
        var timelineEvent = TimelineEvent.Create(
            Guid.NewGuid(),
            "Vertragsabschluss <Test>",
            EventDate.Range(new DateOnly(2025, 12, 1), new DateOnly(2026, 2, 1)),
            Timestamp);
        timelineEvent.UpdateContent(
            timelineEvent.Title,
            "Wichtiger Vorgang",
            "Ausführliche Beschreibung mit Umlauten ÄÖÜ.",
            "Eigene Unterlagen",
            "Nur bei aktiviertem Notizexport",
            Timestamp);
        timelineEvent.SetClassification(EventPriority.High, EventStatus.Active, "#AABBCC", Timestamp);
        timelineEvent.SetDeadline(
            new Deadline(
                Guid.NewGuid(),
                new DateOnly(2026, 1, 15),
                new TimeOnly(10, 30),
                "Abgabe",
                DeadlineStatus.Open,
                "Rechtzeitig prüfen"),
            Timestamp);
        timelineEvent.AddTag("Vertrag", Timestamp);
        timelineEvent.AddTag("Prüfung", Timestamp);
        timelineEvent.AddWebLink(
            new WebLink(Guid.NewGuid(), new Uri("https://example.org/nachweis"), "Externer Nachweis"),
            Timestamp);
        var attachment = new Attachment(
            Guid.NewGuid(),
            "vorschaubild.png",
            "image/png",
            pngData.Length,
            Convert.ToHexString(SHA256.HashData(pngData)).ToLowerInvariant(),
            null,
            Timestamp,
            "attachments/vorschaubild.png");
        timelineEvent.AddAttachment(attachment, Timestamp);
        project.AddEvent(timelineEvent, Timestamp);
        var laterEvent = TimelineEvent.Create(
            Guid.NewGuid(),
            "Spätere Planung",
            EventDate.Exact(new DateOnly(2030, 4, 12)),
            Timestamp);
        laterEvent.UpdateContent(
            laterEvent.Title,
            "Zweiter sichtbarer Eintrag",
            "Dient der kombinierten Browserfilterprüfung.",
            "Eigene Planung",
            null,
            Timestamp);
        laterEvent.SetClassification(EventPriority.Normal, EventStatus.Active, "#DC2626", Timestamp);
        laterEvent.AddTag("Planung", Timestamp);
        project.AddEvent(laterEvent, Timestamp);
        var workspace = CreateWorkspace(project);
        var attachmentFiles = new FixedAttachmentFileService(imagePath);
        var service = new StandaloneHtmlExportService(
            attachmentFiles,
            new UnexpectedPdfPreviewService(),
            new FixedAnalysisStore("Verborgener Dokumentbegriff 4711"));
        var requestedQaPath = Environment.GetEnvironmentVariable("ZEITSTRAHL_HTML_QA_OUTPUT");
        var targetPath = string.IsNullOrWhiteSpace(requestedQaPath)
            ? Path.Combine(temporaryDirectory, "zeitstrahl.html")
            : Path.GetFullPath(requestedQaPath);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        await service.ExportAsync(
            workspace,
            new HtmlExportOptions(TimelineOrientation.Horizontal, true, true),
            targetPath,
            CancellationToken.None);

        var html = await File.ReadAllTextAsync(targetPath);
        Assert.StartsWith("<!doctype html>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Content-Security-Policy", html, StringComparison.Ordinal);
        Assert.Contains("default-src 'none'", html, StringComparison.Ordinal);
        Assert.Contains("@media print", html, StringComparison.Ordinal);
        Assert.Contains("Volltextsuche", html, StringComparison.Ordinal);
        Assert.Contains("horizontalButton", html, StringComparison.Ordinal);
        Assert.Contains("verticalButton", html, StringComparison.Ordinal);
        Assert.Contains("window.confirm", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script src=", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<link rel=", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("</script><img src=x>", html, StringComparison.Ordinal);
        Assert.Contains("\\u003C/script\\u003E\\u003Cimg src=x\\u003E", html, StringComparison.Ordinal);
        Assert.Equal(1, attachmentFiles.ValidationCount);

        using var json = ExtractPayload(html);
        var root = json.RootElement;
        Assert.Equal("Chronik </script><img src=x>", root.GetProperty("name").GetString());
        Assert.Equal("2020-01-01", root.GetProperty("overallStart").GetString());
        Assert.Equal("2030-12-31", root.GetProperty("overallEnd").GetString());
        Assert.Equal("horizontal", root.GetProperty("initialOrientation").GetString());
        var exportedEvent = root.GetProperty("events")[0];
        Assert.Equal("2025-12-01", exportedEvent.GetProperty("startDate").GetString());
        Assert.Equal("2026-02-01", exportedEvent.GetProperty("endDate").GetString());
        Assert.Equal("dateRange", exportedEvent.GetProperty("datePrecision").GetString());
        Assert.Equal("open", exportedEvent.GetProperty("deadline").GetProperty("status").GetString());
        Assert.Contains("Verborgener Dokumentbegriff 4711", exportedEvent.GetProperty("searchText").GetString());
        Assert.Contains("Nur bei aktiviertem Notizexport", exportedEvent.GetProperty("searchText").GetString());
        Assert.StartsWith("data:image/jpeg;base64,", exportedEvent.GetProperty("thumbnailDataUrl").GetString());
        Assert.Equal("https://example.org/nachweis", exportedEvent.GetProperty("webLinks")[0].GetProperty("address").GetString());
    }

    [Fact]
    public async Task ExportAsync_RespectsNotesThumbnailAndOrientationOptions()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Optionstest", Timestamp);
        var timelineEvent = TimelineEvent.Create(
            Guid.NewGuid(),
            "Jahresereignis",
            EventDate.Year(2026),
            Timestamp);
        timelineEvent.UpdateContent(
            timelineEvent.Title,
            null,
            "Beschreibung",
            null,
            "Vertrauliche Notiz",
            Timestamp);
        project.AddEvent(timelineEvent, Timestamp);
        var workspace = CreateWorkspace(project);
        var service = new StandaloneHtmlExportService(
            new UnexpectedAttachmentFileService(),
            new UnexpectedPdfPreviewService(),
            new FixedAnalysisStore(string.Empty));
        var targetPath = Path.Combine(temporaryDirectory, "optionen.html");

        await service.ExportAsync(
            workspace,
            new HtmlExportOptions(TimelineOrientation.Vertical, false, false),
            targetPath,
            CancellationToken.None);

        using var json = ExtractPayload(await File.ReadAllTextAsync(targetPath));
        var root = json.RootElement;
        var exportedEvent = root.GetProperty("events")[0];
        Assert.Equal("vertical", root.GetProperty("initialOrientation").GetString());
        Assert.Equal("2026-01-01", exportedEvent.GetProperty("startDate").GetString());
        Assert.Equal("2026-12-31", exportedEvent.GetProperty("endDate").GetString());
        Assert.Equal("year", exportedEvent.GetProperty("datePrecision").GetString());
        Assert.Equal(JsonValueKind.Null, exportedEvent.GetProperty("notes").ValueKind);
        Assert.Equal(JsonValueKind.Null, exportedEvent.GetProperty("thumbnailDataUrl").ValueKind);
        Assert.DoesNotContain("Vertrauliche Notiz", exportedEvent.GetProperty("searchText").GetString());
    }

    [Fact]
    public async Task ExportAsync_OverwritesAtomicallyRejectsWorkspaceTargetsAndHonorsCancellation()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Sicherer HTML-Export", Timestamp);
        project.AddEvent(
            TimelineEvent.Create(Guid.NewGuid(), "Eintrag", EventDate.Year(2026), Timestamp),
            Timestamp);
        var workspace = CreateWorkspace(project);
        var service = new StandaloneHtmlExportService(
            new UnexpectedAttachmentFileService(),
            new UnexpectedPdfPreviewService(),
            new FixedAnalysisStore(string.Empty));
        var targetPath = Path.Combine(temporaryDirectory, "vorhanden.html");
        await File.WriteAllTextAsync(targetPath, "alt");

        await service.ExportAsync(
            workspace,
            new HtmlExportOptions(TimelineOrientation.Horizontal, false, false),
            targetPath,
            CancellationToken.None);

        Assert.StartsWith("<!doctype html>", await File.ReadAllTextAsync(targetPath), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.EnumerateFiles(temporaryDirectory, "*.tmp"));
        var protectedTarget = Path.Combine(workspace.WorkingDirectory, "projektintern.html");
        var protectedException = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExportAsync(
            workspace,
            new HtmlExportOptions(TimelineOrientation.Horizontal, false, false),
            protectedTarget,
            CancellationToken.None));
        Assert.Contains("Projektarbeitsordners", protectedException.Message, StringComparison.Ordinal);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelledTarget = Path.Combine(temporaryDirectory, "abgebrochen.html");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ExportAsync(
            workspace,
            new HtmlExportOptions(TimelineOrientation.Horizontal, false, false),
            cancelledTarget,
            cancellation.Token));
        Assert.False(File.Exists(cancelledTarget));
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private ProjectWorkspace CreateWorkspace(TimelineProject project)
    {
        var workingDirectory = Path.Combine(temporaryDirectory, "workspace");
        Directory.CreateDirectory(workingDirectory);
        return new ProjectWorkspace(project, workingDirectory, null, false);
    }

    private static JsonDocument ExtractPayload(string html)
    {
        var match = TimelineDataRegex().Match(html);
        Assert.True(match.Success, "Das eingebettete JSON-Datenobjekt wurde nicht gefunden.");
        return JsonDocument.Parse(match.Groups[1].Value);
    }

    [GeneratedRegex(
        """<script id="timelineData" type="application/json">(.*?)</script>""",
        RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex TimelineDataRegex();

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
            throw new InvalidOperationException("Öffnen ist in diesem Test nicht vorgesehen.");
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

    private sealed class FixedAnalysisStore(string extractedText) : IAttachmentAnalysisStore
    {
        public Task SaveAsync(
            ProjectWorkspace workspace,
            Attachment attachment,
            DocumentAnalysisResult result,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Speichern ist in diesem Test nicht vorgesehen.");

        public Task<DocumentAnalysisResult?> LoadAsync(
            ProjectWorkspace workspace,
            Attachment attachment,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<DocumentAnalysisResult?>(new DocumentAnalysisResult(
                attachment.MediaType,
                null,
                extractedText,
                TextExtractionMethod.EmbeddedText,
                new Dictionary<string, string>(),
                [],
                null,
                null));
        }
    }
}
