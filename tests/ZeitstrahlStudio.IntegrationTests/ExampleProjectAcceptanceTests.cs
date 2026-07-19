using System.IO.Compression;
using Microsoft.Data.Sqlite;
using UglyToad.PdfPig;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.DocumentProcessing;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Export;
using ZeitstrahlStudio.Infrastructure;
using ZeitstrahlStudio.SampleGenerator;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class ExampleProjectAcceptanceTests
{
    private static readonly DateTimeOffset EditTimestamp =
        new(2026, 7, 19, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CommittedArchive_FulfillsSampleContractAndExportsPdfAndHtml()
    {
        using var root = new TemporaryRoot();
        var workspace = await ImportAsync(GetCommittedArchivePath(), root.GetPath("imported"));

        AssertSampleContract(workspace.Project);
        using (var archive = ZipFile.OpenRead(GetCommittedArchivePath()))
        {
            Assert.Contains(archive.Entries, entry => entry.FullName == "manifest.json");
            Assert.Contains(archive.Entries, entry => entry.FullName == "project.db");
            Assert.Contains(
                archive.Entries,
                entry => entry.FullName.StartsWith("thumbnails/timeline/", StringComparison.Ordinal));
        }

        var attachments = workspace.Project.Events
            .SelectMany(timelineEvent => timelineEvent.Attachments)
            .ToArray();
        var attachmentFiles = new LocalAttachmentFileService();
        foreach (var attachment in attachments)
        {
            var path = await attachmentFiles.GetValidatedLocalPathAsync(
                workspace,
                attachment,
                CancellationToken.None);
            Assert.True(File.Exists(path));
            Assert.Null(attachment.OriginalSourcePath);
        }

        var pdfAttachment = attachments.First(item => item.MediaType == "application/pdf");
        var pdfPath = await attachmentFiles.GetValidatedLocalPathAsync(
            workspace,
            pdfAttachment,
            CancellationToken.None);
        var preview = await new PdfiumPdfPreviewService().RenderPageAsync(
            pdfPath,
            1,
            0.5,
            CancellationToken.None);
        Assert.True(preview.PngData.Length > 1_000);

        var search = new SqliteProjectSearchService();
        foreach (var term in new[] { "Kupferstern", "Morgenfalter", "Blattgold" })
        {
            var results = await search.SearchAsync(
                workspace,
                new SearchCriteria(Query: term),
                CancellationToken.None);
            Assert.Single(results);
        }

        var audit = await new SqliteAuditLogService().ReadAsync(
            workspace,
            CancellationToken.None);
        Assert.True(audit.Count >= 3);

        var exportDirectory = root.CreateDirectory("exports");
        var pdfExportPath = Path.Combine(exportDirectory, "beispiel.pdf");
        await new SkiaPdfExportService(
                new PdfExportPlanner(),
                attachmentFiles,
                new PdfiumPdfPreviewService())
            .ExportAsync(
                workspace,
                new PdfExportOptions(
                    "A4",
                    Landscape: false,
                    WidthMillimeters: 210,
                    HeightMillimeters: 297,
                    FontSize: 10,
                    RangeStart: null,
                    RangeEnd: null,
                    IncludeOverlappingRanges: true,
                    SingleLargePage: false,
                    IncludeNotes: true),
                pdfExportPath,
                CancellationToken.None);
        using (var document = PdfDocument.Open(pdfExportPath))
        {
            Assert.True(document.NumberOfPages >= 1);
            var text = string.Join(Environment.NewLine, document.GetPages().Select(page => page.Text));
            Assert.Contains("Planungsrunde Nordflügel", text, StringComparison.Ordinal);
        }

        var htmlExportPath = Path.Combine(exportDirectory, "beispiel.html");
        await new StandaloneHtmlExportService(
                attachmentFiles,
                new PdfiumPdfPreviewService(),
                new SqliteAttachmentAnalysisStore())
            .ExportAsync(
                workspace,
                new HtmlExportOptions(TimelineOrientation.Horizontal, true, true),
                htmlExportPath,
                CancellationToken.None);
        var html = await File.ReadAllTextAsync(htmlExportPath);
        Assert.True(html.Length > 20_000);
        Assert.Contains("Kupferstern", html, StringComparison.Ordinal);
        Assert.Contains("Morgenfalter", html, StringComparison.Ordinal);
        Assert.Contains("Blattgold", html, StringComparison.Ordinal);
        Assert.Contains("Content-Security-Policy", html, StringComparison.Ordinal);
        Assert.Contains("@media print", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script src=", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://example.com/zeitstrahl-studio-beispiel", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Generator_CreatesEquivalentArchiveThatRemainsEditableAfterTransfer()
    {
        using var root = new TemporaryRoot();
        var generatedDirectory = root.CreateDirectory("generated");
        var result = await new SampleProjectGenerator().GenerateAsync(
            generatedDirectory,
            CancellationToken.None);
        Assert.Equal(10, result.EventCount);
        Assert.Equal(
            new[] { ".docx", ".pdf", ".pdf", ".png", ".xlsx" },
            result.DocumentPaths.Select(Path.GetExtension).Order().ToArray());

        var generated = await ImportAsync(result.ArchivePath, root.GetPath("generated-import"));
        var committed = await ImportAsync(GetCommittedArchivePath(), root.GetPath("committed-import"));
        Assert.Equal(CreateSemanticSignature(committed.Project), CreateSemanticSignature(generated.Project));

        var eventToEdit = generated.Project.Events.Single(
            timelineEvent => timelineEvent.Title == "Planungsrunde Nordflügel");
        var request = new EventEditRequest(
            eventToEdit.Date,
            eventToEdit.Title + " – geprüft",
            eventToEdit.InfoText,
            eventToEdit.Description,
            eventToEdit.Deadline,
            eventToEdit.Priority,
            eventToEdit.ColorHex,
            eventToEdit.Source,
            eventToEdit.Notes,
            eventToEdit.Status,
            eventToEdit.Tags.ToArray(),
            eventToEdit.WebLinks
                .Select(link => new WebLinkInput(link.Id, link.Address.AbsoluteUri, link.Label))
                .ToArray());
        new ProjectEventEditingService().Update(
            generated.Project,
            eventToEdit.Id,
            request,
            EditTimestamp);
        await new SqliteProjectRepository().SaveAsync(
            generated.Project,
            Path.Combine(generated.WorkingDirectory, "project.db"),
            CancellationToken.None);

        var transferPath = Path.Combine(root.Path, "übertragen.zeitprojekt");
        await new ProjectArchiveService(new SqliteProjectRepository()).ExportAsync(
            generated.WorkingDirectory,
            transferPath,
            progress: null,
            CancellationToken.None);
        var transferred = await ImportAsync(transferPath, root.GetPath("transferred"));
        Assert.Contains(
            transferred.Project.Events,
            timelineEvent => timelineEvent.Title == "Planungsrunde Nordflügel – geprüft");
        Assert.Equal(
            generated.Project.Events.SelectMany(item => item.Attachments).Select(item => item.Sha256).Order(),
            transferred.Project.Events.SelectMany(item => item.Attachments).Select(item => item.Sha256).Order());
        AssertSampleContract(transferred.Project);

        var searchResults = await new SqliteProjectSearchService().SearchAsync(
            transferred,
            new SearchCriteria(Query: "Morgenfalter"),
            CancellationToken.None);
        Assert.Single(searchResults);
    }

    private static async Task<ProjectWorkspace> ImportAsync(string archivePath, string target)
    {
        var result = await new ProjectArchiveService(new SqliteProjectRepository()).ImportAsync(
            archivePath,
            target,
            progress: null,
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.TechnicalDetails);
        return result.Value!.Workspace;
    }

    private static void AssertSampleContract(TimelineProject project)
    {
        Assert.Equal(10, project.Events.Count);
        Assert.All(Enum.GetValues<DatePrecision>(), precision =>
            Assert.Contains(project.Events, timelineEvent => timelineEvent.Date.Precision == precision));
        var sameDate = project.Events
            .Where(item => item.Date == EventDate.Exact(new DateOnly(2024, 4, 12)))
            .OrderBy(item => item.ManualSortPosition)
            .ToArray();
        Assert.Equal(2, sameDate.Length);
        Assert.Equal(new decimal?[] { 10m, 20m }, sameDate.Select(item => item.ManualSortPosition));
        Assert.True(project.Events.Count(item => item.Deadline is not null) >= 3);
        Assert.All(Enum.GetValues<DeadlineStatus>(), status =>
            Assert.Contains(project.Events, item => item.Deadline?.Status == status));
        Assert.True(project.Events.Select(item => item.ColorHex).Distinct().Count() >= 4);
        Assert.True(project.Events.Select(item => item.Priority).Distinct().Count() >= 3);
        Assert.True(project.Events.SelectMany(item => item.Tags).Distinct().Count() >= 8);
        Assert.Contains(
            project.Events.SelectMany(item => item.WebLinks),
            link => link.Address.Host == "example.com");
        Assert.Equal(5, project.Events.SelectMany(item => item.Attachments).Count());
        Assert.Contains(project.LayoutPositions, item => item.Orientation == TimelineOrientation.Horizontal);
        Assert.Contains(project.LayoutPositions, item => item.Orientation == TimelineOrientation.Vertical);
        var dates = project.GetChronologicalEvents().Select(item => item.Date.SortStart).ToArray();
        Assert.Contains(dates.Zip(dates.Skip(1)), pair =>
            (pair.Second - pair.First).TotalDays > 20 * 365);
    }

    private static string CreateSemanticSignature(TimelineProject project) => string.Join(
        Environment.NewLine,
        project.GetChronologicalEvents().Select(timelineEvent => string.Join(
            "|",
            timelineEvent.Id,
            timelineEvent.Date,
            timelineEvent.Title,
            timelineEvent.Priority,
            timelineEvent.Status,
            timelineEvent.ColorHex,
            timelineEvent.ManualSortPosition,
            string.Join(',', timelineEvent.Tags.Order()),
            string.Join(',', timelineEvent.Attachments
                .OrderBy(item => item.OriginalFileName)
                .Select(item => $"{item.OriginalFileName}:{item.MediaType}")))));

    private static string GetCommittedArchivePath() => Path.Combine(
        AppContext.BaseDirectory,
        "SampleData",
        SampleProjectGenerator.ArchiveFileName);

    private sealed class TemporaryRoot : IDisposable
    {
        public TemporaryRoot()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "ZeitstrahlStudio.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string GetPath(string name) => System.IO.Path.Combine(Path, name);

        public string CreateDirectory(string name)
        {
            var path = System.IO.Path.Combine(Path, name);
            Directory.CreateDirectory(path);
            return path;
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
