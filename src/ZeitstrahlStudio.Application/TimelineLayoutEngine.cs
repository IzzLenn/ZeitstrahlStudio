using System.Globalization;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Application;

/// <summary>Automatisch gewählte Beschriftungseinheit der Zeitachse.</summary>
public enum TimelineScaleUnit
{
    Hours,
    Days,
    Weeks,
    Months,
    Years,
    Decades,
}

/// <summary>Geometrische und fachliche Optionen einer Zeitstrahlberechnung.</summary>
public sealed record TimelineLayoutOptions(
    TimelineOrientation Orientation,
    double ZoomFactor = 1,
    bool CompressLargeGaps = true,
    double ViewportAxisLength = 1000,
    double ViewportCrossLength = 600,
    double CardFontSize = 14);

/// <summary>Position einer Ereigniskarte relativ zur Zeitachse.</summary>
public sealed record TimelineCardLayout(
    Guid EventId,
    double AnchorAxisPosition,
    double AxisPosition,
    double CrossPosition,
    double AxisLength,
    double CrossLength,
    bool IsPositiveSide,
    int Lane,
    bool HasManualPosition,
    bool HasManualConflict = false);

/// <summary>Eindeutig beschriftete Kompression einer leeren Zeitspanne.</summary>
public sealed record TimelineAxisBreak(
    DateTime GapStart,
    DateTime GapEnd,
    double AxisStart,
    double AxisEnd,
    string Label);

/// <summary>Skalenmarkierung auf der projizierten Zeitachse.</summary>
public sealed record TimelineAxisTick(
    DateTime Value,
    double AxisPosition,
    string Label,
    bool IsMajor);

/// <summary>Unabhängiger Fristmarker samt Verbindung zum zugehörigen Ereignis.</summary>
public sealed record TimelineDeadlineLayout(
    Guid EventId,
    Guid DeadlineId,
    double EventAxisPosition,
    double AxisPosition,
    DeadlineStatus Status,
    string Label);

/// <summary>Vollständiges, UI-unabhängiges Layout eines Zeitstrahls.</summary>
public sealed record TimelineLayoutResult(
    TimelineOrientation Orientation,
    TimelineScaleUnit ScaleUnit,
    DateTime Start,
    DateTime End,
    double ContentAxisLength,
    double ContentCrossLength,
    IReadOnlyList<TimelineCardLayout> Cards,
    IReadOnlyList<TimelineAxisBreak> Breaks,
    IReadOnlyList<TimelineAxisTick> Ticks,
    IReadOnlyList<TimelineDeadlineLayout> Deadlines);

/// <summary>
/// Projiziert fachliche Datumswerte deterministisch auf eine Achse, komprimiert große Lücken
/// und verteilt überlappende Karten auf abwechselnde Bahnen.
/// </summary>
public sealed class TimelineLayoutEngine
{
    private const double AxisPadding = 180;
    private const double CardSpacing = 22;
    private const double AxisToCardSpacing = 38;
    private const double MaximumManualOffset = 100_000;
    private const int MaximumTicks = 2_000;

    public TimelineLayoutResult Create(
        TimelineProject project,
        TimelineLayoutOptions options,
        IReadOnlySet<Guid>? visibleEventIds = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ValidateOptions(options);
        var chronologicalEvents = project.GetChronologicalEvents();
        IReadOnlyList<TimelineEvent> events = visibleEventIds is null
            ? chronologicalEvents
            : chronologicalEvents.Where(timelineEvent => visibleEventIds.Contains(timelineEvent.Id)).ToArray();
        if (events.Count == 0)
        {
            return Empty(options);
        }

        var anchors = CollectAnchors(project, events);
        var start = anchors[0];
        var end = anchors[^1];
        var scaleUnit = SelectScaleUnit(end - start);
        var pixelsPerDay = GetPixelsPerDay(scaleUnit) * options.ZoomFactor;
        var gaps = options.CompressLargeGaps
            ? DetectLargeGaps(anchors, events, scaleUnit, pixelsPerDay, options.ZoomFactor)
            : [];
        var projection = new AxisProjection(start, pixelsPerDay, gaps);
        var (cardAxisLength, cardCrossLength) = GetCardSize(
            options.Orientation,
            options.CardFontSize);
        var positions = project.LayoutPositions
            .Where(position => position.Orientation == options.Orientation)
            .ToDictionary(position => position.EventId);
        var cards = PlaceCards(
            events,
            projection,
            positions,
            options.Orientation,
            cardAxisLength,
            cardCrossLength);
        var breaks = gaps.Select(gap => new TimelineAxisBreak(
            gap.Start,
            gap.End,
            projection.Map(gap.Start),
            projection.Map(gap.End),
            FormatGap(gap.Start, gap.End))).ToArray();
        var deadlines = events
            .Where(timelineEvent => timelineEvent.Deadline is not null)
            .Select(timelineEvent => CreateDeadlineLayout(timelineEvent, projection, cards))
            .ToArray();
        var ticks = CreateTicks(start, end, scaleUnit, projection, gaps);
        var maximumAxis = Math.Max(
            projection.Map(end),
            cards.Max(card => card.AxisPosition + (card.AxisLength / 2)));
        if (deadlines.Length > 0)
        {
            maximumAxis = Math.Max(maximumAxis, deadlines.Max(deadline => deadline.AxisPosition));
        }

        var maximumCross = cards.Max(card => Math.Abs(card.CrossPosition) + (card.CrossLength / 2));
        return new TimelineLayoutResult(
            options.Orientation,
            scaleUnit,
            start,
            end,
            Math.Max(options.ViewportAxisLength, maximumAxis + AxisPadding),
            Math.Max(options.ViewportCrossLength, (maximumCross + AxisPadding) * 2),
            cards,
            breaks,
            ticks,
            deadlines);
    }

    /// <summary>
    /// Projiziert ein frei gewähltes Datum mit denselben Skalierungs- und Lückenregeln wie das Layout.
    /// Werte außerhalb des Projektzeitraums werden an dessen Grenzen begrenzt.
    /// </summary>
    public double GetAxisPosition(
        TimelineProject project,
        TimelineLayoutOptions options,
        DateTime value)
    {
        ArgumentNullException.ThrowIfNull(project);
        ValidateOptions(options);
        var events = project.GetChronologicalEvents();
        if (events.Count == 0)
        {
            return AxisPadding;
        }

        var anchors = CollectAnchors(project, events);
        var start = anchors[0];
        var end = anchors[^1];
        var scaleUnit = SelectScaleUnit(end - start);
        var pixelsPerDay = GetPixelsPerDay(scaleUnit) * options.ZoomFactor;
        var gaps = options.CompressLargeGaps
            ? DetectLargeGaps(anchors, events, scaleUnit, pixelsPerDay, options.ZoomFactor)
            : [];
        var projection = new AxisProjection(start, pixelsPerDay, gaps);
        return projection.Map(value < start ? start : value > end ? end : value);
    }

    private static TimelineLayoutResult Empty(TimelineLayoutOptions options)
    {
        var today = DateTime.Today;
        return new TimelineLayoutResult(
            options.Orientation,
            TimelineScaleUnit.Days,
            today,
            today,
            options.ViewportAxisLength,
            options.ViewportCrossLength,
            [],
            [],
            [],
            []);
    }

    private static IReadOnlyList<DateTime> CollectAnchors(
        TimelineProject project,
        IReadOnlyList<TimelineEvent> events)
    {
        var anchors = new List<DateTime>(events.Count * 3 + 2);
        foreach (var timelineEvent in events)
        {
            anchors.Add(timelineEvent.Date.SortStart);
            anchors.Add(GetEventEnd(timelineEvent.Date));
            if (timelineEvent.Deadline is { } deadline)
            {
                anchors.Add(deadline.DueDate.ToDateTime(deadline.DueTime ?? TimeOnly.MinValue));
            }
        }

        if (project.OverallStart is { } overallStart)
        {
            anchors.Add(overallStart.ToDateTime(TimeOnly.MinValue));
        }

        if (project.OverallEnd is { } overallEnd)
        {
            anchors.Add(overallEnd.ToDateTime(TimeOnly.MaxValue));
        }

        return anchors.Distinct().Order().ToArray();
    }

    private static IReadOnlyList<TimelineCardLayout> PlaceCards(
        IReadOnlyList<TimelineEvent> events,
        AxisProjection projection,
        IReadOnlyDictionary<Guid, LayoutPosition> positions,
        TimelineOrientation orientation,
        double cardAxisLength,
        double cardCrossLength)
    {
        var positiveLanes = new List<double>();
        var negativeLanes = new List<double>();
        var result = new List<TimelineCardLayout>(events.Count);
        for (var index = 0; index < events.Count; index++)
        {
            var timelineEvent = events[index];
            var automaticAxis = projection.Map(timelineEvent.Date.SortStart);
            var positiveSide = index % 2 == 0;
            var lanes = positiveSide ? positiveLanes : negativeLanes;
            var lane = FindLane(lanes, automaticAxis, cardAxisLength);
            var cross = AxisToCardSpacing + (cardCrossLength / 2) +
                (lane * (cardCrossLength + CardSpacing));
            if (!positiveSide)
            {
                cross = -cross;
            }

            var hasManualPosition = positions.TryGetValue(timelineEvent.Id, out var manual);
            var axisOffset = 0d;
            var crossOffset = 0d;
            if (manual is not null)
            {
                axisOffset = orientation == TimelineOrientation.Horizontal
                    ? manual.HorizontalOffset
                    : manual.VerticalOffset;
                crossOffset = orientation == TimelineOrientation.Horizontal
                    ? manual.VerticalOffset
                    : manual.HorizontalOffset;
                axisOffset = Math.Clamp(axisOffset, -MaximumManualOffset, MaximumManualOffset);
                crossOffset = Math.Clamp(crossOffset, -MaximumManualOffset, MaximumManualOffset);
            }

            var adjustedCross = cross + crossOffset;
            result.Add(new TimelineCardLayout(
                timelineEvent.Id,
                automaticAxis,
                Math.Max((cardAxisLength / 2) + CardSpacing, automaticAxis + axisOffset),
                adjustedCross,
                cardAxisLength,
                cardCrossLength,
                adjustedCross >= 0,
                lane,
                hasManualPosition));
        }

        MarkManualConflicts(result);
        return result;
    }

    private static void MarkManualConflicts(List<TimelineCardLayout> cards)
    {
        if (!cards.Any(card => card.HasManualPosition))
        {
            return;
        }

        for (var first = 0; first < cards.Count; first++)
        {
            for (var second = first + 1; second < cards.Count; second++)
            {
                var left = cards[first];
                var right = cards[second];
                if ((!left.HasManualPosition && !right.HasManualPosition) ||
                    !CardsOverlap(left, right))
                {
                    continue;
                }

                cards[first] = left with { HasManualConflict = true };
                cards[second] = right with { HasManualConflict = true };
            }
        }
    }

    private static bool CardsOverlap(TimelineCardLayout first, TimelineCardLayout second) =>
        Math.Abs(first.AxisPosition - second.AxisPosition) <
            ((first.AxisLength + second.AxisLength) / 2) + CardSpacing &&
        Math.Abs(first.CrossPosition - second.CrossPosition) <
            ((first.CrossLength + second.CrossLength) / 2) + CardSpacing;

    private static int FindLane(List<double> laneEnds, double axisPosition, double cardAxisLength)
    {
        var cardStart = axisPosition - (cardAxisLength / 2);
        for (var lane = 0; lane < laneEnds.Count; lane++)
        {
            if (cardStart >= laneEnds[lane] + CardSpacing)
            {
                laneEnds[lane] = axisPosition + (cardAxisLength / 2);
                return lane;
            }
        }

        laneEnds.Add(axisPosition + (cardAxisLength / 2));
        return laneEnds.Count - 1;
    }

    private static TimelineDeadlineLayout CreateDeadlineLayout(
        TimelineEvent timelineEvent,
        AxisProjection projection,
        IReadOnlyList<TimelineCardLayout> cards)
    {
        var deadline = timelineEvent.Deadline!;
        var value = deadline.DueDate.ToDateTime(deadline.DueTime ?? TimeOnly.MinValue);
        var eventPosition = cards.Single(card => card.EventId == timelineEvent.Id).AnchorAxisPosition;
        return new TimelineDeadlineLayout(
            timelineEvent.Id,
            deadline.Id,
            eventPosition,
            projection.Map(value),
            deadline.Status,
            string.IsNullOrWhiteSpace(deadline.Label) ? "Frist" : deadline.Label);
    }

    private static IReadOnlyList<GapDefinition> DetectLargeGaps(
        IReadOnlyList<DateTime> anchors,
        IReadOnlyList<TimelineEvent> events,
        TimelineScaleUnit scaleUnit,
        double pixelsPerDay,
        double zoomFactor)
    {
        var thresholdDays = scaleUnit switch
        {
            TimelineScaleUnit.Hours => 2,
            TimelineScaleUnit.Days => 60,
            TimelineScaleUnit.Weeks => 365,
            TimelineScaleUnit.Months => 730,
            TimelineScaleUnit.Years => 3650,
            TimelineScaleUnit.Decades => 18_250,
            _ => throw new InvalidOperationException("Die Zeitskala wird nicht unterstützt."),
        };
        var compressedPixels = 150 * Math.Sqrt(zoomFactor);
        var result = new List<GapDefinition>();
        for (var index = 1; index < anchors.Count; index++)
        {
            var days = (anchors[index] - anchors[index - 1]).TotalDays;
            var coveredByEventRange = events.Any(timelineEvent =>
                timelineEvent.Date.SortStart <= anchors[index - 1] &&
                GetEventEnd(timelineEvent.Date) >= anchors[index]);
            if (!coveredByEventRange &&
                days > thresholdDays &&
                days * pixelsPerDay > compressedPixels * 2)
            {
                result.Add(new GapDefinition(anchors[index - 1], anchors[index], compressedPixels));
            }
        }

        return result;
    }

    private static IReadOnlyList<TimelineAxisTick> CreateTicks(
        DateTime start,
        DateTime end,
        TimelineScaleUnit scaleUnit,
        AxisProjection projection,
        IReadOnlyList<GapDefinition> gaps)
    {
        if (start == end)
        {
            return [new TimelineAxisTick(start, projection.Map(start), FormatTick(start, scaleUnit), true)];
        }

        var ticks = new List<TimelineAxisTick>();
        var current = AlignTick(start, scaleUnit);
        for (var index = 0; index < MaximumTicks && current <= end; index++)
        {
            if (current >= start && !gaps.Any(gap => current > gap.Start && current < gap.End))
            {
                ticks.Add(new TimelineAxisTick(
                    current,
                    projection.Map(current),
                    FormatTick(current, scaleUnit),
                    IsMajorTick(current, scaleUnit)));
            }

            var next = IncrementTick(current, scaleUnit);
            if (next <= current)
            {
                break;
            }

            current = next;
        }

        return ticks;
    }

    private static TimelineScaleUnit SelectScaleUnit(TimeSpan span) => span.TotalDays switch
    {
        <= 3 => TimelineScaleUnit.Hours,
        <= 120 => TimelineScaleUnit.Days,
        <= 730 => TimelineScaleUnit.Weeks,
        <= 3_652 => TimelineScaleUnit.Months,
        <= 36_525 => TimelineScaleUnit.Years,
        _ => TimelineScaleUnit.Decades,
    };

    private static double GetPixelsPerDay(TimelineScaleUnit scaleUnit) => scaleUnit switch
    {
        TimelineScaleUnit.Hours => 96,
        TimelineScaleUnit.Days => 28,
        TimelineScaleUnit.Weeks => 8,
        TimelineScaleUnit.Months => 2.5,
        TimelineScaleUnit.Years => 0.25,
        TimelineScaleUnit.Decades => 0.035,
        _ => throw new InvalidOperationException("Die Zeitskala wird nicht unterstützt."),
    };

    private static (double AxisLength, double CrossLength) GetCardSize(
        TimelineOrientation orientation,
        double cardFontSize)
    {
        var width = 260 * Math.Sqrt(Math.Max(1, cardFontSize / 14));
        var height = Math.Max(132, 24 + (cardFontSize * 8));
        return orientation == TimelineOrientation.Horizontal
            ? (width, height)
            : (height, width);
    }

    private static DateTime GetEventEnd(EventDate date) => date.EndYear is null
        ? date.SortStart
        : new DateTime(date.EndYear.Value, date.EndMonth!.Value, date.EndDay!.Value);

    private static DateTime AlignTick(DateTime value, TimelineScaleUnit scaleUnit) => scaleUnit switch
    {
        TimelineScaleUnit.Hours => new DateTime(
            value.Year,
            value.Month,
            value.Day,
            value.Hour - (value.Hour % 6),
            0,
            0),
        TimelineScaleUnit.Days or TimelineScaleUnit.Weeks => value.Date,
        TimelineScaleUnit.Months => new DateTime(value.Year, value.Month, 1),
        TimelineScaleUnit.Years => new DateTime(value.Year, 1, 1),
        TimelineScaleUnit.Decades => new DateTime(Math.Max(1, value.Year - (value.Year % 10)), 1, 1),
        _ => throw new InvalidOperationException("Die Zeitskala wird nicht unterstützt."),
    };

    private static DateTime IncrementTick(DateTime value, TimelineScaleUnit scaleUnit)
    {
        if (value.Year == 9999)
        {
            return value;
        }

        return scaleUnit switch
        {
            TimelineScaleUnit.Hours => value.AddHours(6),
            TimelineScaleUnit.Days => value.AddDays(7),
            TimelineScaleUnit.Weeks => value.AddDays(28),
            TimelineScaleUnit.Months => value.AddMonths(1),
            TimelineScaleUnit.Years => value.AddYears(1),
            TimelineScaleUnit.Decades => value.Year > 9989
                ? new DateTime(9999, 1, 1)
                : value.AddYears(10),
            _ => throw new InvalidOperationException("Die Zeitskala wird nicht unterstützt."),
        };
    }

    private static bool IsMajorTick(DateTime value, TimelineScaleUnit scaleUnit) => scaleUnit switch
    {
        TimelineScaleUnit.Hours => value.Hour == 0,
        TimelineScaleUnit.Days or TimelineScaleUnit.Weeks => value.Day <= 7,
        TimelineScaleUnit.Months => value.Month == 1,
        TimelineScaleUnit.Years => value.Year % 10 == 0,
        TimelineScaleUnit.Decades => value.Year % 100 == 0,
        _ => false,
    };

    private static string FormatTick(DateTime value, TimelineScaleUnit scaleUnit) => scaleUnit switch
    {
        TimelineScaleUnit.Hours => value.ToString("dd.MM. HH:mm", CultureInfo.InvariantCulture),
        TimelineScaleUnit.Days or TimelineScaleUnit.Weeks =>
            value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
        TimelineScaleUnit.Months => value.ToString("MMM yyyy", CultureInfo.GetCultureInfo("de-DE")),
        TimelineScaleUnit.Years or TimelineScaleUnit.Decades =>
            value.Year.ToString(CultureInfo.InvariantCulture),
        _ => throw new InvalidOperationException("Die Zeitskala wird nicht unterstützt."),
    };

    private static string FormatGap(DateTime start, DateTime end)
    {
        var startDate = DateOnly.FromDateTime(start);
        var endDate = DateOnly.FromDateTime(end);
        var totalMonths = ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month;
        if (endDate.Day < startDate.Day)
        {
            totalMonths--;
        }

        totalMonths = Math.Max(0, totalMonths);
        var cursor = startDate.AddMonths(totalMonths);
        var days = endDate.DayNumber - cursor.DayNumber;
        var years = totalMonths / 12;
        var months = totalMonths % 12;
        var parts = new List<string>(3);
        if (years > 0)
        {
            parts.Add(years == 1 ? "1 Jahr" : $"{years} Jahre");
        }

        if (months > 0)
        {
            parts.Add(months == 1 ? "1 Monat" : $"{months} Monate");
        }

        if (days > 0 || parts.Count == 0)
        {
            parts.Add(days == 1 ? "1 Tag" : $"{days} Tage");
        }

        return "Unterbrechung: " + string.Join(" und ", parts);
    }

    private static void ValidateOptions(TimelineLayoutOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!double.IsFinite(options.ZoomFactor) || options.ZoomFactor is < 0.25 or > 8)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Der Zeitstrahlzoom muss zwischen 25 und 800 Prozent liegen.");
        }

        if (!double.IsFinite(options.CardFontSize) || options.CardFontSize is < 8 or > 48)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Die Kartenschriftgröße muss zwischen 8 und 48 Punkt liegen.");
        }

        if (!double.IsFinite(options.ViewportAxisLength) || options.ViewportAxisLength <= 0 ||
            !double.IsFinite(options.ViewportCrossLength) || options.ViewportCrossLength <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Die Größe des Zeitstrahlfensters muss positiv und endlich sein.");
        }
    }

    private sealed record GapDefinition(DateTime Start, DateTime End, double CompressedPixels);

    private sealed class AxisProjection(
        DateTime start,
        double pixelsPerDay,
        IReadOnlyList<GapDefinition> gaps)
    {
        public double Map(DateTime value)
        {
            var position = AxisPadding + ((value - start).TotalDays * pixelsPerDay);
            foreach (var gap in gaps)
            {
                var rawPixels = (gap.End - gap.Start).TotalDays * pixelsPerDay;
                if (value >= gap.End)
                {
                    position -= rawPixels - gap.CompressedPixels;
                    continue;
                }

                if (value > gap.Start)
                {
                    var fraction = (value - gap.Start).TotalDays /
                        (gap.End - gap.Start).TotalDays;
                    position -= ((value - gap.Start).TotalDays * pixelsPerDay) -
                        (gap.CompressedPixels * fraction);
                }

                break;
            }

            return position;
        }
    }
}
