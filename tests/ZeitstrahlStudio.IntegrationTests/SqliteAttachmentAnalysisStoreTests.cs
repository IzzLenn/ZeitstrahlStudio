using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Infrastructure;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class SqliteAttachmentAnalysisStoreTests : IDisposable
{
    private static readonly DateTimeOffset BaseTime =
        new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "ZeitstrahlStudio.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAndLoadAsync_PersistsResultAndRefreshesFullTextIndex()
    {
        Directory.CreateDirectory(directory);
        var project = TimelineProject.Create(Guid.NewGuid(), "Analyseprojekt", BaseTime);
        var timelineEvent = TimelineEvent.Create(
            Guid.NewGuid(),
            "Dokument",
            EventDate.Year(2026),
            BaseTime);
        var attachment = new Attachment(
            Guid.NewGuid(),
            "quelle.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            12,
            new string('c', 64),
            null,
            BaseTime,
            $"attachments/{Guid.NewGuid():N}/quelle.docx");
        timelineEvent.AddAttachment(attachment, BaseTime.AddMinutes(1));
        project.AddEvent(timelineEvent, BaseTime.AddMinutes(2));
        var databasePath = Path.Combine(directory, "project.db");
        var repository = new SqliteProjectRepository();
        await repository.SaveAsync(project, databasePath, CancellationToken.None);
        var workspace = new ProjectWorkspace(project, directory, null, HasUnsavedChanges: true);
        var result = new DocumentAnalysisResult(
            attachment.MediaType,
            "Quellentitel",
            "Ein seltener Findebegriff 19.07.2026",
            TextExtractionMethod.OfficeDocument,
            new Dictionary<string, string> { ["creator"] = "Autor" },
            ["19.07.2026"],
            "thumbnails/vorschau.png",
            3);
        var store = new SqliteAttachmentAnalysisStore();

        await store.SaveAsync(workspace, attachment, result, CancellationToken.None);
        var loadedResult = await store.LoadAsync(workspace, attachment, CancellationToken.None);

        Assert.NotNull(loadedResult);
        Assert.Equal(result.MediaType, loadedResult.MediaType);
        Assert.Equal(result.Title, loadedResult.Title);
        Assert.Equal(result.ExtractedText, loadedResult.ExtractedText);
        Assert.Equal(result.ExtractionMethod, loadedResult.ExtractionMethod);
        Assert.Equal("Autor", loadedResult.Metadata["creator"]);
        Assert.Equal(result.DateSuggestions, loadedResult.DateSuggestions);
        Assert.Equal(result.ThumbnailRelativePath, loadedResult.ThumbnailRelativePath);
        Assert.Equal(result.PageCount, loadedResult.PageCount);
        var loadedProject = await repository.LoadAsync(databasePath, CancellationToken.None);
        Assert.Equal(
            AttachmentState.Ready,
            Assert.Single(Assert.Single(loadedProject.Events).Attachments).State);

        await using var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM SearchIndex WHERE SearchIndex MATCH 'Findebegriff';";
        Assert.Equal(1L, (long)(await command.ExecuteScalarAsync())!);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
