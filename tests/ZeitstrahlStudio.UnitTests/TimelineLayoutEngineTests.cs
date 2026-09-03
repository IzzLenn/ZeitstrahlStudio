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
        Assert.Equal(automatic.AnchorAxisPosition, adjusted.AnchorAxisPosition, precision: 6);
        Assert.Equal(expectedCrossDelta, adjusted.CrossPosition - automatic.CrossPosition, precision: 6);
        Assert.True(adjusted.HasManualPosition);
        Assert.Equal(new DateTime(2026, 7, 19), timelineEvent.Date.SortStart);
    }

    [Theory]
    [InlineData(TimelineOrientation.Horizontal, 50, -25)]
    [InlineData(TimelineOrientation.Vertical, -25, 50)]
    public void Create_KeepsManualPositionStableAcrossZoomLevels(
        TimelineOrientation orientation,
        double canonicalAxisOffset,
        double expectedCrossOffset)
    {
        var project = CreateProject(
            EventDate.Exact(new DateOnly(2026, 7, 1)),
            EventDate.Exact(new DateOnly(2026, 7, 3)),
            EventDate.Exact(new DateOnly(2026, 7, 5)));
        var timelineEvent = project.Events[2];
        var automaticAtOneHundredPercent = engine.Create(
            project,
            new TimelineLayoutOptions(orientation, ZoomFactor: 1));
        var automaticAtEightHundredPercent = engine.Create(
            project,
            new TimelineLayoutOptions(orientation, ZoomFactor: 8));
        var automaticCard = automaticAtOneHundredPercent.Cards.Single(
            card => card.EventId == timelineEvent.Id);
        var automaticZoomedCard = automaticAtEightHundredPercent.Cards.Single(
            card => card.EventId == timelineEvent.Id);
        project.SetLayoutPosition(
            new LayoutPosition(
                timelineEvent.Id,
                orientation,
                horizontalOffset: 50,
                verticalOffset: -25),
            Timestamp);
        var storedPosition = Assert.Single(project.LayoutPositions);

        var atOneHundredPercent = engine.Create(
            project,
            new TimelineLayoutOptions(orientation, ZoomFactor: 1));
        var atEightHundredPercent = engine.Create(
            project,
            new TimelineLayoutOptions(orientation, ZoomFactor: 8));
        var backAtOneHundredPercent = engine.Create(
            project,
            new TimelineLayoutOptions(orientation, ZoomFactor: 1));
        var baselineCard = atOneHundredPercent.Cards.Single(card => card.EventId == timelineEvent.Id);
        var zoomedCard = atEightHundredPercent.Cards.Single(card => card.EventId == timelineEvent.Id);
        var restoredCard = backAtOneHundredPercent.Cards.Single(card => card.EventId == timelineEvent.Id);

        Assert.Equal(canonicalAxisOffset, baselineCard.AxisPosition - baselineCard.AnchorAxisPosition, precision: 6);
        Assert.Equal(
            canonicalAxisOffset,
            (zoomedCard.AxisPosition - zoomedCard.AnchorAxisPosition) / 8,
            precision: 6);
        Assert.Equal(baselineCard.CrossPosition, zoomedCard.CrossPosition, precision: 6);
        Assert.Equal(
            expectedCrossOffset,
            baselineCard.CrossPosition - automaticCard.CrossPosition,
            precision: 6);
        Assert.NotEqual(automaticCard.Lane, automaticZoomedCard.Lane);
        Assert.Equal(baselineCard, restoredCard);
        Assert.Same(storedPosition, Assert.Single(project.LayoutPositions));
        Assert.Equal(new DateTime(2026, 7, 5), timelineEvent.Date.SortStart);
    }

    [Fact]
    public void Create_MarksManualCardOverlapAsConflict()
    {
        var date = EventDate.Exact(new DateOnly(2026, 7, 19));
        var project = CreateProject(date, date);
        var automatic = engine.Create(
            project,
            new TimelineLayoutOptions(TimelineOrientation.Horizontal));
        var first = automatic.Cards[0];
        var second = automatic.Cards[1];
        var secondEvent = project.Events.Single(timelineEvent => timelineEvent.Id == second.EventId);
        project.SetLayoutPosition(
            new LayoutPosition(
                secondEvent.Id,
                TimelineOrientation.Horizontal,
                horizontalOffset: first.AxisPosition - second.AxisPosition,
                verticalOffset: first.CrossPosition - second.CrossPosition),
            Timestamp);

        var adjusted = engine.Create(
            project,
            new TimelineLayoutOptions(TimelineOrientation.Horizontal));

        Assert.All(adjusted.Cards, card => Assert.True(card.HasManualConflict));
        Assert.Contains(adjusted.Cards, card => card.HasManualPosition);
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

    [Fact]
    public void GetAxisPosition_UsesTheSameCompressedProjectionAndClampsOutsideValues()
    {
        var project = CreateProject(
            EventDate.Exact(new DateOnly(1900, 1, 1)),
            EventDate.Exact(new DateOnly(2000, 1, 1)));
        var options = new TimelineLayoutOptions(
            TimelineOrientation.Horizontal,
            ZoomFactor: 1.5,
            CompressLargeGaps: true);
        var layout = engine.Create(project, options);

        var before = engine.GetAxisPosition(project, options, new DateTime(1800, 1, 1));
        var middle = engine.GetAxisPosition(project, options, new DateTime(1950, 1, 1));
        var after = engine.GetAxisPosition(project, options, new DateTime(2100, 1, 1));

        Assert.Equal(layout.Cards[0].AxisPosition, before, precision: 6);
        Assert.InRange(middle, before, after);
        Assert.Equal(layout.Cards[1].AxisPosition, after, precision: 6);
    }

    [Fact]
    public void Create_WithVisibleEventIdsReturnsOnlyMatchingCards()
    {
        var project = CreateProject(
            EventDate.Year(1990),
            EventDate.Year(2000),
            EventDate.Year(2010));
        var visibleId = project.Events[1].Id;

        var result = engine.Create(
            project,
            new TimelineLayoutOptions(TimelineOrientation.Horizontal),
            new HashSet<Guid> { visibleId });

        Assert.Equal(visibleId, Assert.Single(result.Cards).EventId);
    }

    [Fact]
    public void Create_ExpandsCardsForConfiguredCardFontSize()
    {
        var project = CreateProject(EventDate.Year(2026));
        var normal = Assert.Single(engine.Create(
            project,
            new TimelineLayoutOptions(
                TimelineOrientation.Horizontal,
                CardFontSize: 14)).Cards);
        var large = Assert.Single(engine.Create(
            project,
            new TimelineLayoutOptions(
                TimelineOrientation.Horizontal,
                CardFontSize: 28)).Cards);

        Assert.True(large.AxisLength > normal.AxisLength);
        Assert.True(large.CrossLength > normal.CrossLength);
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
