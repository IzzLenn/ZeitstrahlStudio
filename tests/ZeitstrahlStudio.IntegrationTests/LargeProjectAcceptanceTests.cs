using System.Diagnostics;
using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Infrastructure;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class LargeProjectAcceptanceTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task FiveThousandEvents_RoundTripSearchAndLayoutRemainBounded()
    {
        using var root = new TemporaryRoot();
        var project = CreateProject(eventCount: 5_000);
        var databasePath = Path.Combine(root.Path, "project.db");
        var repository = new SqliteProjectRepository();
        var stopwatch = Stopwatch.StartNew();

        await repository.SaveAsync(project, databasePath, CancellationToken.None);
        var loaded = await repository.LoadAsync(databasePath, CancellationToken.None);

        Assert.Equal(5_000, loaded.Events.Count);
        Assert.Equal(40, loaded.Events.SelectMany(item => item.Attachments).Count());
        Assert.True(stopwatch.Elapsed < TimeSpan.FromMinutes(2));

        var workspace = new ProjectWorkspace(
            loaded,
            root.Path,
            ArchivePath: null,
            HasUnsavedChanges: false);
        var results = await new SqliteProjectSearchService().SearchAsync(
            workspace,
            new SearchCriteria(Query: "Lastanker4999"),
            CancellationToken.None);
        Assert.Equal("Großprojekt-Ereignis 5000", Assert.Single(results).EventTitle);

        var engine = new TimelineLayoutEngine();
        foreach (var orientation in Enum.GetValues<TimelineOrientation>())
        {
            var layout = engine.Create(
                loaded,
                new TimelineLayoutOptions(
                    orientation,
                    ZoomFactor: 1,
                    CompressLargeGaps: true,
                    ViewportAxisLength: 1_600,
                    ViewportCrossLength: 900,
                    CardFontSize: 14));
            Assert.Equal(5_000, layout.Cards.Count);
            Assert.True(layout.Ticks.Count <= 2_000);
            Assert.True(double.IsFinite(layout.ContentAxisLength));
            Assert.True(double.IsFinite(layout.ContentCrossLength));
            Assert.All(layout.Cards, card =>
            {
                Assert.True(double.IsFinite(card.AxisPosition));
                Assert.True(double.IsFinite(card.CrossPosition));
            });
        }

        Assert.True(stopwatch.Elapsed < TimeSpan.FromMinutes(2));
    }

    private static TimelineProject CreateProject(int eventCount)
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Lasttest mit 5.000 Ereignissen", Timestamp);
        var firstDate = new DateOnly(1900, 1, 1);
        for (var index = 0; index < eventCount; index++)
        {
            var date = firstDate.AddDays(index * 3);
            var eventDate = (index % 100) switch
            {
                0 => EventDate.Year(date.Year),
                1 => EventDate.MonthAndYear(date.Year, date.Month),
                2 => EventDate.Range(date, date.AddDays(10)),
                3 => EventDate.ExactWithTime(date, new TimeOnly(8, 30)),
                _ => EventDate.Exact(date),
            };
            var timelineEvent = TimelineEvent.Create(
                Guid.NewGuid(),
                $"Großprojekt-Ereignis {index + 1}",
                eventDate,
                Timestamp);
            timelineEvent.UpdateContent(
                timelineEvent.Title,
                $"Sachverhalt {index + 1}",
                index == eventCount - 1
                    ? "Eindeutiger Suchbegriff Lastanker4999 für den letzten Datensatz."
                    : $"Lokale Testbeschreibung Nummer {index + 1}.",
                "Automatisierter lokaler Lasttest",
                null,
                Timestamp.AddSeconds(1));
            timelineEvent.SetClassification(
                (EventPriority)(index % Enum.GetValues<EventPriority>().Length),
                EventStatus.Active,
                index % 2 == 0 ? "#2563EB" : "#B45309",
                Timestamp.AddSeconds(1));
            timelineEvent.AddTag($"Gruppe-{index % 20:D2}", Timestamp.AddSeconds(1));
            if (index % 500 == 0)
            {
                timelineEvent.SetDeadline(
                    new Deadline(
                        Guid.NewGuid(),
                        date.AddDays(30),
                        label: $"Frist {index + 1}",
                        status: DeadlineStatus.Open),
                    Timestamp.AddSeconds(1));
            }

            if (index % 250 == 0)
            {
                for (var attachmentIndex = 0; attachmentIndex < 2; attachmentIndex++)
                {
                    timelineEvent.AddAttachment(
                        new Attachment(
                            Guid.NewGuid(),
                            $"nachweis-{index:D4}-{attachmentIndex}.pdf",
                            "application/pdf",
                            1_024 + index,
                            new string(attachmentIndex == 0 ? 'a' : 'b', 64),
                            originalSourcePath: null,
                            Timestamp,
                            $"attachments/{timelineEvent.Id:N}/nachweis-{attachmentIndex}.pdf",
                            AttachmentState.Ready),
                        Timestamp.AddSeconds(1));
                }
            }

            project.AddEvent(timelineEvent, Timestamp.AddSeconds(2));
        }

        return project;
    }

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
