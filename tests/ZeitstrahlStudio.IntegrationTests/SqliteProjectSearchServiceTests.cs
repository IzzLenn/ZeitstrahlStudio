using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Infrastructure;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class SqliteProjectSearchServiceTests : IDisposable
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "ZeitstrahlStudio.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SearchAsync_FindsExtractedDocumentPrefixAndReturnsMarkedHighlight()
    {
        var (workspace, documentedEvent, _) = await CreateWorkspaceAsync();
        var attachment = Assert.Single(documentedEvent.Attachments);
        var analysisStore = new SqliteAttachmentAnalysisStore();
        await analysisStore.SaveAsync(
            workspace,
            attachment,
            new DocumentAnalysisResult(
                attachment.MediaType,
                "Quellentitel",
                "Die friedliche Wiedervereinigung wurde dokumentiert.",
                TextExtractionMethod.EmbeddedText,
                new Dictionary<string, string>(),
                [],
                ThumbnailRelativePath: null,
                PageCount: 2),
            CancellationToken.None);
        var service = new SqliteProjectSearchService();

        var results = await service.SearchAsync(
            workspace,
            new SearchCriteria(Query: "Wiederver"),
            CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal(documentedEvent.Id, result.EventId);
        Assert.Contains(result.Highlights, highlight =>
            highlight.Contains("⟦Wiederver", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchAsync_CombinesAllStructuredFilters()
    {
        var (workspace, documentedEvent, plainEvent) = await CreateWorkspaceAsync();
        var service = new SqliteProjectSearchService();

        var results = await service.SearchAsync(
            workspace,
            new SearchCriteria(
                Query: "Projektchronik",
                From: new DateOnly(1989, 1, 1),
                Until: new DateOnly(1991, 12, 31),
                Precision: DatePrecision.DateRange,
                HasDeadline: true,
                DeadlineStatus: DeadlineStatus.Open,
                Priority: EventPriority.High,
                ColorHex: "#AA1122",
                Tag: "Geschichte",
                MediaType: "application/pdf",
                HasAttachment: true,
                HasPdf: true),
            CancellationToken.None);

        Assert.Equal(documentedEvent.Id, Assert.Single(results).EventId);

        var withoutAttachments = await service.SearchAsync(
            workspace,
            new SearchCriteria(HasAttachment: false, HasPdf: false),
            CancellationToken.None);
        Assert.Equal(plainEvent.Id, Assert.Single(withoutAttachments).EventId);
    }

    [Fact]
    public async Task SearchAsync_SeesUnsavedAggregateTextAndRejectsInvalidRange()
    {
        var (workspace, documentedEvent, _) = await CreateWorkspaceAsync();
        documentedEvent.UpdateContent(
            "Ungespeicherter Sonderbegriff",
            documentedEvent.InfoText,
            documentedEvent.Description,
            documentedEvent.Source,
            documentedEvent.Notes,
            Timestamp.AddMinutes(10));
        var service = new SqliteProjectSearchService();

        var results = await service.SearchAsync(
            workspace with { HasUnsavedChanges = true },
            new SearchCriteria(Query: "Sonderbegriff"),
            CancellationToken.None);

        Assert.Equal(documentedEvent.Id, Assert.Single(results).EventId);
        var staleResults = await service.SearchAsync(
            workspace with { HasUnsavedChanges = true },
            new SearchCriteria(Query: "Mauerfall"),
            CancellationToken.None);
        Assert.Empty(staleResults);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SearchAsync(
                workspace,
                new SearchCriteria(
                    From: new DateOnly(2020, 1, 2),
                    Until: new DateOnly(2020, 1, 1)),
                CancellationToken.None));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private async Task<(ProjectWorkspace Workspace, TimelineEvent Documented, TimelineEvent Plain)>
        CreateWorkspaceAsync()
    {
        Directory.CreateDirectory(directory);
        var project = TimelineProject.Create(Guid.NewGuid(), "Projektchronik", Timestamp);
        project.UpdateInformation(
            project.Name,
            subtitle: null,
            infoText: null,
            description: "Deutsche Zeitgeschichte",
            overallStart: null,
            overallEnd: null,
            Timestamp);
        var documented = TimelineEvent.Create(
            Guid.NewGuid(),
            "Mauerfall",
            EventDate.Range(new DateOnly(1989, 11, 9), new DateOnly(1990, 10, 3)),
            Timestamp);
        documented.UpdateContent(
            documented.Title,
            "Historischer Wendepunkt",
            "Öffnung der innerdeutschen Grenze",
            "Bundesarchiv",
            "Quellen geprüft",
            Timestamp);
        documented.SetClassification(
            EventPriority.High,
            EventStatus.Active,
            "#AA1122",
            Timestamp);
        documented.AddTag("Geschichte", Timestamp);
        documented.SetDeadline(
            new Deadline(
                Guid.NewGuid(),
                new DateOnly(2027, 1, 15),
                label: "Auswertung",
                status: DeadlineStatus.Open),
            Timestamp);
        documented.AddAttachment(
            new Attachment(
                Guid.NewGuid(),
                "quelle.pdf",
                "application/pdf",
                42,
                new string('a', 64),
                originalSourcePath: null,
                Timestamp,
                $"attachments/{documented.Id:N}/quelle.pdf"),
            Timestamp);
        var plain = TimelineEvent.Create(
            Guid.NewGuid(),
            "Gegenwart",
            EventDate.Exact(new DateOnly(2020, 1, 1)),
            Timestamp);
        plain.SetClassification(
            EventPriority.Normal,
            EventStatus.Active,
            "#336699",
            Timestamp);
        project.AddEvent(documented, Timestamp);
        project.AddEvent(plain, Timestamp);
        var repository = new SqliteProjectRepository();
        await repository.SaveAsync(
            project,
            Path.Combine(directory, "project.db"),
            CancellationToken.None);
        return (
            new ProjectWorkspace(project, directory, ArchivePath: null, HasUnsavedChanges: false),
            documented,
            plain);
    }
}
