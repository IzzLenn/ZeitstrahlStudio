using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.UnitTests;

public sealed class TimelineLayoutEngineTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);
    private readonly TimelineLayoutEngine engine = new();

    [Theory]
    [InlineData(2000, 1, 1, 2000, 1, 3, TimelineScaleUnit.Hours)]
    [InlineData(2000, 1, 1, 2000, 2, 1, TimelineScaleUnit.Days)]
    [InlineData(2000, 1, 1, 2001, 1, 1, TimelineScaleUnit.Weeks)]
    [InlineData(2000, 1, 1, 2005, 1, 1, TimelineScaleUnit.Months)]
    [InlineData(1900, 1, 1, 1950, 1, 1, TimelineScaleUnit.Years)]
    [InlineData(1700, 1, 1, 2000, 1, 1, TimelineScaleUnit.Decades)]
    public void Create_SelectsScaleFromProjectSpan(
        int startYear,
        int startMonth,
        int startDay,
        int endYear,
        int endMonth,
        int endDay,
        TimelineScaleUnit expected)
    {
        var project = CreateProject(
            EventDate.Exact(new DateOnly(startYear, startMonth, startDay)),
            EventDate.Exact(new DateOnly(endYear, endMonth, endDay)));

        var result = engine.Create(
            project,
            new TimelineLayoutOptions(TimelineOrientation.Horizontal));

        Assert.Equal(expected, result.ScaleUnit);
        Assert.Equal(2, result.Cards.Count);
        Assert.True(result.Cards[1].AxisPosition > result.Cards[0].AxisPosition);
        Assert.NotEmpty(result.Ticks);
    }

    [Fact]
    public void Create_CompressesAndLabelsLargeEmptyGap()
    {
        var project = CreateProject(
            EventDate.Exact(new DateOnly(2000, 1, 1)),
            EventDate.Exact(new DateOnly(2001, 1, 1)),
            EventDate.Exact(new DateOnly(2020, 4, 1)));

        var compressed = engine.Create(
            project,
            new TimelineLayoutOptions(
                TimelineOrientation.Horizontal,
                CompressLargeGaps: true));
        var uncompressed = engine.Create(
            project,
            new TimelineLayoutOptions(
                TimelineOrientation.Horizontal,
                CompressLargeGaps: false));

        var axisBreak = Assert.Single(compressed.Breaks);
        Assert.Contains("Unterbrechung:", axisBreak.Label);
        Assert.Contains("19 Jahre", axisBreak.Label);
        Assert.True(axisBreak.AxisEnd > axisBreak.AxisStart);
        Assert.True(
            compressed.Cards[^1].AxisPosition < uncompressed.Cards[^1].AxisPosition / 2,
            "Die große Lücke muss die projizierte Achsenlänge deutlich reduzieren.");
        Assert.Empty(uncompressed.Breaks);
    }

    [Fact]
    public void Create_DoesNotCompressTimeCoveredByEventRange()
    {
        var project = CreateProject(EventDate.Range(
            new DateOnly(2000, 1, 1),
            new DateOnly(2020, 4, 1)));

        var result = engine.Create(
            project,
            new TimelineLayoutOptions(
                TimelineOrientation.Horizontal,
                CompressLargeGaps: true));

        Assert.Empty(result.Breaks);
        Assert.True(result.ContentAxisLength > 1_000);
    }

    [Fact]
    public void Create_AlternatesSidesAndUsesAdditionalLanesForEqualDates()
    {
        var date = EventDate.Exact(new DateOnly(2026, 7, 19));
        var project = CreateProject(date, date, date, date);

        var result = engine.Create(
            project,
            new TimelineLayoutOptions(TimelineOrientation.Vertical));

        Assert.Equal([true, false, true, false], result.Cards.Select(card => card.IsPositiveSide));
        Assert.Equal([0, 0, 1, 1], result.Cards.Select(card => card.Lane));
        Assert.Equal(4, result.Cards.Select(card => card.CrossPosition).Distinct().Count());
        Assert.True(result.ContentCrossLength > 600);
    }

    [Theory]
    [InlineData(TimelineOrientation.Horizontal, 50, -25)]
    [InlineData(TimelineOrientation.Vertical, -25, 50)]
    public void Create_AppliesOrientationSpecificManualOffsetsWithoutChangingDate(
        TimelineOrientation orientation,
        double expectedAxisDelta,
        double expectedCrossDelta)
    {
        var project = CreateProject(EventDate.Exact(new DateOnly(2026, 7, 19)));
        var timelineEvent = Assert.Single(project.Events);
        var automatic = Assert.Single(engine.Create(
            project,
            new TimelineLayoutOptions(orientation)).Cards);
        project.SetLayoutPosition(
            new LayoutPosition(
                timelineEvent.Id,
                orientation,
                horizontalOffset: 50,
                verticalOffset: -25),
            Timestamp);

        var adjusted = Assert.Single(engine.Create(
            project,
            new TimelineLayoutOptions(orientation)).Cards);

        Assert.Equal(expectedAxisDelta, adjusted.AxisPosition - automatic.AxisPosition, precision: 6);
        Assert.Equal(expectedCrossDelta, adjusted.CrossPosition - automatic.CrossPosition, precision: 6);
        Assert.True(adjusted.HasManualPosition);
        Assert.Equal(new DateTime(2026, 7, 19), timelineEvent.Date.SortStart);
    }

    [Fact]
    public void Create_ProducesIndependentDeadlineMarkerAndConnectionCoordinates()
    {
        var project = CreateProject(EventDate.Exact(new DateOnly(2026, 7, 1)));
        var timelineEvent = Assert.Single(project.Events);
        timelineEvent.SetDeadline(
            new Deadline(
                Guid.NewGuid(),
                new DateOnly(2026, 7, 19),
                new TimeOnly(16, 30),
                "Einreichung",
                DeadlineStatus.Open),
            Timestamp);

        var result = engine.Create(
            project,
            new TimelineLayoutOptions(TimelineOrientation.Horizontal));

        var marker = Assert.Single(result.Deadlines);
        var card = Assert.Single(result.Cards);
        Assert.Equal(timelineEvent.Id, marker.EventId);
        Assert.Equal("Einreichung", marker.Label);
        Assert.Equal(card.AxisPosition, marker.EventAxisPosition);
        Assert.True(marker.AxisPosition > marker.EventAxisPosition);
    }

    [Fact]
    public void Create_RemainsFiniteForFiveThousandEvents()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Lasttest", Timestamp);
        for (var index = 0; index < 5_000; index++)
        {
            var date = new DateOnly(1900, 1, 1).AddDays(index * 3);
            project.AddEvent(CreateEvent(index, EventDate.Exact(date)), Timestamp);
        }

        var result = engine.Create(
            project,
            new TimelineLayoutOptions(
                TimelineOrientation.Horizontal,
                ZoomFactor: 1.25,
                CompressLargeGaps: true,
                ViewportAxisLength: 1_200,
                ViewportCrossLength: 700));

        Assert.Equal(5_000, result.Cards.Count);
        Assert.InRange(result.Ticks.Count, 1, 2_000);
        Assert.True(double.IsFinite(result.ContentAxisLength));
        Assert.True(double.IsFinite(result.ContentCrossLength));
        Assert.All(result.Cards, card =>
        {
            Assert.True(double.IsFinite(card.AxisPosition));
            Assert.True(double.IsFinite(card.CrossPosition));
        });
    }

    private static TimelineProject CreateProject(params EventDate[] dates)
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Layouttest", Timestamp);
        for (var index = 0; index < dates.Length; index++)
        {
            project.AddEvent(CreateEvent(index, dates[index]), Timestamp);
        }

        return project;
    }

    private static TimelineEvent CreateEvent(int index, EventDate date) =>
        TimelineEvent.Create(Guid.NewGuid(), $"Ereignis {index + 1}", date, Timestamp);
}
