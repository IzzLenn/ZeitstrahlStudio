using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Export;

namespace ZeitstrahlStudio.UnitTests;

public sealed class PdfExportPlannerTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);
    private readonly PdfExportPlanner planner = new();

    [Fact]
    public void Create_PaginatesWithoutSplittingOrdinaryCards()
    {
        var project = CreateProject(24);

        var plan = planner.Create(project, DefaultOptions());

        Assert.True(plan.Pages.Count > 1);
        Assert.All(plan.Pages, page => Assert.NotEmpty(page.EventBlocks));
        Assert.Equal(
            project.Events.Select(item => item.Id).Order(),
            plan.Pages.SelectMany(page => page.EventBlocks).Select(block => block.EventId).Order());
        Assert.DoesNotContain(
            plan.Pages.SelectMany(page => page.EventBlocks),
            block => block.IsContinuation || block.ContinuesOnNextPage);
    }

    [Fact]
    public void Create_PreservesVeryLongContentAcrossMarkedContinuations()
    {
        var project = CreateProject(1);
        var timelineEvent = project.Events[0];
        timelineEvent.UpdateContent(
            timelineEvent.Title,
            "Zusammenfassung",
            string.Join(' ', Enumerable.Repeat("ausführlicher unverzichtbarer Inhalt", 500)),
            "Lokale Quelle",
            "Interne Notiz",
            Timestamp);

        var plan = planner.Create(project, DefaultOptions() with { IncludeNotes = true });
        var blocks = plan.Pages.SelectMany(page => page.EventBlocks).ToArray();

        Assert.True(blocks.Length > 1);
        Assert.True(blocks[0].ContinuesOnNextPage);
        Assert.Contains(blocks.Skip(1), block => block.IsContinuation);
        Assert.Contains(plan.Warnings, warning => warning.Contains("Fortsetzung", StringComparison.Ordinal));
        Assert.Contains(
            blocks.SelectMany(block => block.Lines),
            line => line.Text.Contains("unverzichtbarer Inhalt", StringComparison.Ordinal));
    }

    [Fact]
    public void Create_RangeIncludesOverlapAndIndependentDeadline()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Zeitraum", Timestamp);
        var overlapping = CreateEvent(
            "Überlappender Zeitraum",
            EventDate.Range(new DateOnly(2025, 12, 1), new DateOnly(2026, 2, 1)));
        var deadlineOnly = CreateEvent("Frühes Ereignis", EventDate.Exact(new DateOnly(2020, 1, 1)));
        deadlineOnly.SetDeadline(
            new Deadline(Guid.NewGuid(), new DateOnly(2026, 1, 15), label: "Abgabe"),
            Timestamp);
        var outside = CreateEvent("Außerhalb", EventDate.Exact(new DateOnly(2027, 1, 1)));
        project.AddEvent(overlapping, Timestamp);
        project.AddEvent(deadlineOnly, Timestamp);
        project.AddEvent(outside, Timestamp);

        var plan = planner.Create(project, DefaultOptions() with
        {
            RangeStart = new DateOnly(2026, 1, 1),
            RangeEnd = new DateOnly(2026, 1, 31),
            IncludeOverlappingRanges = true,
        });
        var ids = plan.Pages.SelectMany(page => page.EventBlocks).Select(block => block.EventId).ToHashSet();

        Assert.Contains(overlapping.Id, ids);
        Assert.Contains(deadlineOnly.Id, ids);
        Assert.DoesNotContain(outside.Id, ids);
    }

    [Fact]
    public void Create_SingleLargePageReportsActualSizeAndViewerWarning()
    {
        var project = CreateProject(100);

        var plan = planner.Create(project, DefaultOptions() with { SingleLargePage = true });

        Assert.Single(plan.Pages);
        Assert.True(plan.HeightMillimeters > 1_000);
        Assert.Equal(plan.HeightMillimeters, plan.Pages[0].HeightPoints * 25.4 / 72, precision: 2);
        Assert.Contains(plan.Warnings, warning => warning.Contains("PDF-Betrachter", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false, 210, 297)]
    [InlineData(true, 297, 210)]
    public void Create_ResolvesA4Orientation(bool landscape, double expectedWidth, double expectedHeight)
    {
        var plan = planner.Create(
            CreateProject(1),
            DefaultOptions() with { Landscape = landscape });

        Assert.Equal(expectedWidth, plan.WidthMillimeters, precision: 3);
        Assert.Equal(expectedHeight, plan.HeightMillimeters, precision: 3);
    }

    [Fact]
    public void Create_RejectsIncompleteOrReversedRange()
    {
        var project = CreateProject(1);

        Assert.Throws<ArgumentException>(() => planner.Create(
            project,
            DefaultOptions() with { RangeStart = new DateOnly(2026, 1, 1) }));
        Assert.Throws<ArgumentException>(() => planner.Create(
            project,
            DefaultOptions() with
            {
                RangeStart = new DateOnly(2026, 2, 1),
                RangeEnd = new DateOnly(2026, 1, 1),
            }));
    }

    private static TimelineProject CreateProject(int eventCount)
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "PDF-Testprojekt", Timestamp);
        for (var index = 0; index < eventCount; index++)
        {
            var timelineEvent = CreateEvent(
                $"Ereignis {index + 1}",
                EventDate.Exact(new DateOnly(2026, 1, 1).AddDays(index)));
            timelineEvent.UpdateContent(
                timelineEvent.Title,
                "Kurzinformation",
                "Eine ausreichend ausführliche Beschreibung für den druckoptimierten Export.",
                "Eigene Unterlagen",
                null,
                Timestamp);
            project.AddEvent(timelineEvent, Timestamp);
        }

        return project;
    }

    private static TimelineEvent CreateEvent(string title, EventDate date) =>
        TimelineEvent.Create(Guid.NewGuid(), title, date, Timestamp);

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
}
