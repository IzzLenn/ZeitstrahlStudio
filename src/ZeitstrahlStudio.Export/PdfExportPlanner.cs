using System.Globalization;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Export;

/// <summary>Semantische Textart innerhalb einer druckbaren Ereigniskarte.</summary>
public enum PdfTextRole
{
    Date,
    Title,
    Body,
    Metadata,
    Deadline,
}

/// <summary>Eine bereits umbrochene Textzeile für den vektorbasierten PDF-Renderer.</summary>
public sealed record PdfTextLine(string Text, PdfTextRole Role);

/// <summary>Eine Ereigniskarte oder ein Fortsetzungsabschnitt auf einer PDF-Seite.</summary>
public sealed record PdfEventBlockPlan(
    Guid EventId,
    float TopPoints,
    float HeightPoints,
    bool IsContinuation,
    bool ContinuesOnNextPage,
    bool HasThumbnailCandidate,
    IReadOnlyList<PdfTextLine> Lines);

/// <summary>Deterministische Planung einer einzelnen PDF-Seite.</summary>
public sealed record PdfPagePlan(
    int PageNumber,
    float WidthPoints,
    float HeightPoints,
    IReadOnlyList<PdfEventBlockPlan> EventBlocks);

/// <summary>Vollständige Seitenplanung einschließlich druckrelevanter Warnungen.</summary>
public sealed record PdfExportPlan(
    double WidthMillimeters,
    double HeightMillimeters,
    float FontSizePoints,
    IReadOnlyList<PdfPagePlan> Pages,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Filtert Ereignisse, bricht Inhalte um und verteilt Karten ohne Inhaltsverlust auf PDF-Seiten.
/// </summary>
public sealed class PdfExportPlanner
{
    public const float MarginPoints = 34;
    public const float HeaderHeightPoints = 52;
    public const float FooterHeightPoints = 22;
    public const float AxisLaneWidthPoints = 34;
    public const float CardGapPoints = 12;
    public const float CardPaddingPoints = 10;
    public const float ThumbnailWidthPoints = 72;
    public const float ThumbnailGapPoints = 9;

    private const double PointsPerMillimeter = 72d / 25.4d;
    private const double MinimumPaperMillimeters = 50;
    private const double MaximumPaperMillimeters = 5_080;
    private const double LargePageWarningMillimeters = 1_000;

    public PdfExportPlan Create(TimelineProject project, PdfExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);

        var (widthMillimeters, baseHeightMillimeters) = ResolvePaperSize(options);
        var widthPoints = ToPoints(widthMillimeters);
        var baseHeightPoints = ToPoints(baseHeightMillimeters);
        var fontSize = (float)options.FontSize;
        var events = FilterEvents(project, options);
        var warnings = new List<string>();
        if (events.Count == 0)
        {
            warnings.Add("Der gewählte Exportbereich enthält keine Ereignisse oder Fristen.");
        }

        var preparedEvents = events
            .Select(timelineEvent => PrepareEvent(timelineEvent, options, widthPoints, fontSize))
            .ToArray();

        if (options.SingleLargePage)
        {
            var naturalHeight = HeaderHeightPoints + FooterHeightPoints + (MarginPoints * 2);
            naturalHeight += preparedEvents.Sum(item => MeasureBlock(item.Lines, fontSize));
            naturalHeight += Math.Max(0, preparedEvents.Length - 1) * CardGapPoints;
            var heightPoints = Math.Max(baseHeightPoints, naturalHeight);
            var heightMillimeters = heightPoints / PointsPerMillimeter;
            if (heightMillimeters > MaximumPaperMillimeters)
            {
                throw new InvalidOperationException(
                    "Der Zeitstrahl ist für eine einzelne PDF-Seite höher als 5.080 mm. " +
                    "Bitte verwenden Sie den mehrseitigen Export oder einen kleineren Exportbereich.");
            }

            if (widthMillimeters > LargePageWarningMillimeters ||
                heightMillimeters > LargePageWarningMillimeters)
            {
                warnings.Add(
                    "Die große Einzelseite überschreitet 1.000 mm. Manche PDF-Betrachter oder Drucker " +
                    "können diese Seitengröße nur eingeschränkt verarbeiten.");
            }

            var top = MarginPoints + HeaderHeightPoints;
            var blocks = new List<PdfEventBlockPlan>(preparedEvents.Length);
            foreach (var prepared in preparedEvents)
            {
                var blockHeight = MeasureBlock(prepared.Lines, fontSize);
                blocks.Add(new PdfEventBlockPlan(
                    prepared.Event.Id,
                    top,
                    blockHeight,
                    false,
                    false,
                    prepared.HasThumbnailCandidate,
                    prepared.Lines));
                top += blockHeight + CardGapPoints;
            }

            var page = new PdfPagePlan(1, widthPoints, heightPoints, blocks);
            return new PdfExportPlan(
                widthMillimeters,
                heightMillimeters,
                fontSize,
                [page],
                warnings);
        }

        var pages = Paginate(preparedEvents, widthPoints, baseHeightPoints, fontSize, warnings);
        return new PdfExportPlan(
            widthMillimeters,
            baseHeightMillimeters,
            fontSize,
            pages,
            warnings);
    }

    private static IReadOnlyList<PdfPagePlan> Paginate(
        IReadOnlyList<PreparedEvent> events,
        float widthPoints,
        float heightPoints,
        float fontSize,
        List<string> warnings)
    {
        var pages = new List<PdfPagePlan>();
        var currentBlocks = new List<PdfEventBlockPlan>();
        var top = MarginPoints + HeaderHeightPoints;
        var contentBottom = heightPoints - MarginPoints - FooterHeightPoints;
        var splitEventIds = new HashSet<Guid>();

        void CompletePage()
        {
            pages.Add(new PdfPagePlan(pages.Count + 1, widthPoints, heightPoints, currentBlocks.ToArray()));
            currentBlocks = [];
            top = MarginPoints + HeaderHeightPoints;
        }

        foreach (var prepared in events)
        {
            var remainingLines = prepared.Lines.ToList();
            var continuation = false;
            while (remainingLines.Count > 0)
            {
                var remainingHeight = contentBottom - top;
                var repeatedHeader = continuation
                    ? CreateContinuationHeader(prepared)
                    : [];
                var completeLines = repeatedHeader.Concat(remainingLines).ToArray();
                var completeHeight = MeasureBlock(completeLines, fontSize);
                if (completeHeight <= remainingHeight)
                {
                    currentBlocks.Add(new PdfEventBlockPlan(
                        prepared.Event.Id,
                        top,
                        completeHeight,
                        continuation,
                        false,
                        !continuation && prepared.HasThumbnailCandidate,
                        completeLines));
                    top += completeHeight + CardGapPoints;
                    remainingLines.Clear();
                    continue;
                }

                var fullPageContentHeight = contentBottom -
                    (MarginPoints + HeaderHeightPoints);
                if (currentBlocks.Count > 0 && completeHeight <= fullPageContentHeight)
                {
                    CompletePage();
                    continue;
                }

                var availableForContent = remainingHeight - (CardPaddingPoints * 2) -
                    repeatedHeader.Sum(line => LineHeight(line.Role, fontSize));
                var fitCount = CountFittingLines(remainingLines, availableForContent, fontSize);
                if (fitCount < 2 && currentBlocks.Count > 0)
                {
                    CompletePage();
                    continue;
                }

                if (fitCount == 0)
                {
                    throw new InvalidOperationException(
                        "Das gewählte Papierformat ist zu klein für eine druckbare Ereigniskarte.");
                }

                var chunkLines = repeatedHeader.Concat(remainingLines.Take(fitCount)).ToArray();
                remainingLines.RemoveRange(0, fitCount);
                var blockHeight = MeasureBlock(chunkLines, fontSize);
                currentBlocks.Add(new PdfEventBlockPlan(
                    prepared.Event.Id,
                    top,
                    blockHeight,
                    continuation,
                    remainingLines.Count > 0,
                    !continuation && prepared.HasThumbnailCandidate,
                    chunkLines));
                splitEventIds.Add(prepared.Event.Id);
                continuation = true;
                CompletePage();
            }
        }

        if (currentBlocks.Count > 0 || pages.Count == 0)
        {
            CompletePage();
        }

        if (splitEventIds.Count > 0)
        {
            warnings.Add(
                splitEventIds.Count == 1
                    ? "Eine sehr lange Ereigniskarte wird mit eindeutig gekennzeichneter Fortsetzung ausgegeben."
                    : $"{splitEventIds.Count.ToString(CultureInfo.InvariantCulture)} sehr lange Ereigniskarten werden mit eindeutig gekennzeichneten Fortsetzungen ausgegeben.");
        }

        return pages;
    }

    private static PreparedEvent PrepareEvent(
        TimelineEvent timelineEvent,
        PdfExportOptions options,
        float pageWidth,
        float fontSize)
    {
        var hasThumbnail = timelineEvent.Attachments.Any(attachment =>
            attachment.MediaType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
            attachment.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));
        var cardWidth = pageWidth - (MarginPoints * 2) - AxisLaneWidthPoints;
        var textWidth = cardWidth - (CardPaddingPoints * 2);
        if (hasThumbnail)
        {
            textWidth -= ThumbnailWidthPoints + ThumbnailGapPoints;
        }

        var lines = new List<PdfTextLine>();
        AddWrapped(lines, timelineEvent.Date.ToDisplayString(), PdfTextRole.Date, textWidth, fontSize);
        AddWrapped(lines, timelineEvent.Title, PdfTextRole.Title, textWidth, fontSize);
        AddOptional(lines, timelineEvent.InfoText, PdfTextRole.Body, textWidth, fontSize);
        AddLabelled(lines, "Beschreibung", timelineEvent.Description, PdfTextRole.Body, textWidth, fontSize);
        if (timelineEvent.Deadline is { } deadline)
        {
            var deadlineText = $"Frist: {deadline.DueDate:dd.MM.yyyy}";
            if (deadline.DueTime is { } dueTime)
            {
                deadlineText += $" {dueTime:HH\\:mm}";
            }

            if (!string.IsNullOrWhiteSpace(deadline.Label))
            {
                deadlineText += $" · {deadline.Label}";
            }

            deadlineText += $" · {TranslateDeadlineStatus(deadline.Status)}";
            AddWrapped(lines, deadlineText, PdfTextRole.Deadline, textWidth, fontSize);
            AddLabelled(lines, "Fristhinweis", deadline.ReminderNote, PdfTextRole.Metadata, textWidth, fontSize);
        }

        AddWrapped(
            lines,
            $"Priorität: {TranslatePriority(timelineEvent.Priority)} · Status: {TranslateEventStatus(timelineEvent.Status)}",
            PdfTextRole.Metadata,
            textWidth,
            fontSize);
        if (timelineEvent.Tags.Count > 0)
        {
            AddWrapped(
                lines,
                "Schlagwörter: " + string.Join(", ", timelineEvent.Tags.Order(StringComparer.CurrentCultureIgnoreCase)),
                PdfTextRole.Metadata,
                textWidth,
                fontSize);
        }

        AddLabelled(lines, "Quelle", timelineEvent.Source, PdfTextRole.Metadata, textWidth, fontSize);
        if (options.IncludeNotes)
        {
            AddLabelled(lines, "Notizen", timelineEvent.Notes, PdfTextRole.Body, textWidth, fontSize);
        }

        if (timelineEvent.Attachments.Count > 0)
        {
            AddWrapped(
                lines,
                "Dokumente: " + string.Join(", ", timelineEvent.Attachments.Select(item => item.OriginalFileName)),
                PdfTextRole.Metadata,
                textWidth,
                fontSize);
        }

        return new PreparedEvent(timelineEvent, hasThumbnail, lines);
    }

    private static IReadOnlyList<PdfTextLine> CreateContinuationHeader(PreparedEvent prepared)
    {
        return
        [
            new PdfTextLine(prepared.Event.Date.ToDisplayString(), PdfTextRole.Date),
            new PdfTextLine(prepared.Event.Title + " (Fortsetzung)", PdfTextRole.Title),
        ];
    }

    private static void AddOptional(
        List<PdfTextLine> target,
        string? value,
        PdfTextRole role,
        float width,
        float fontSize)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            AddWrapped(target, value, role, width, fontSize);
        }
    }

    private static void AddLabelled(
        List<PdfTextLine> target,
        string label,
        string? value,
        PdfTextRole role,
        float width,
        float fontSize)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            AddWrapped(target, $"{label}: {value}", role, width, fontSize);
        }
    }

    private static void AddWrapped(
        List<PdfTextLine> target,
        string value,
        PdfTextRole role,
        float width,
        float fontSize)
    {
        var normalized = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length == 0)
        {
            return;
        }

        var roleSize = RoleFontSize(role, fontSize);
        // Ein konservatives Em pro Zeichen verhindert auch bei breiten Großbuchstaben und
        // Dateinamen ein horizontales Abschneiden im Renderer.
        var maximumCharacters = Math.Max(8, (int)Math.Floor(width / roleSize));
        var remaining = normalized;
        while (remaining.Length > maximumCharacters)
        {
            var split = remaining.LastIndexOf(' ', maximumCharacters);
            if (split < maximumCharacters / 3)
            {
                split = maximumCharacters;
            }

            target.Add(new PdfTextLine(remaining[..split].TrimEnd(), role));
            remaining = remaining[split..].TrimStart();
        }

        if (remaining.Length > 0)
        {
            target.Add(new PdfTextLine(remaining, role));
        }
    }

    private static float MeasureBlock(IReadOnlyList<PdfTextLine> lines, float fontSize) =>
        (CardPaddingPoints * 2) + lines.Sum(line => LineHeight(line.Role, fontSize));

    private static int CountFittingLines(
        IReadOnlyList<PdfTextLine> lines,
        float availableHeight,
        float fontSize)
    {
        var used = 0f;
        for (var index = 0; index < lines.Count; index++)
        {
            var height = LineHeight(lines[index].Role, fontSize);
            if (used + height > availableHeight)
            {
                return index;
            }

            used += height;
        }

        return lines.Count;
    }

    public static float RoleFontSize(PdfTextRole role, float fontSize) => role switch
    {
        PdfTextRole.Title => fontSize + 2,
        PdfTextRole.Date => Math.Max(8, fontSize - 0.5f),
        PdfTextRole.Metadata => Math.Max(8, fontSize - 1),
        PdfTextRole.Deadline => fontSize,
        _ => fontSize,
    };

    public static float LineHeight(PdfTextRole role, float fontSize) =>
        RoleFontSize(role, fontSize) * (role == PdfTextRole.Title ? 1.42f : 1.35f);

    private static IReadOnlyList<TimelineEvent> FilterEvents(
        TimelineProject project,
        PdfExportOptions options)
    {
        if (!options.RangeStart.HasValue)
        {
            return project.GetChronologicalEvents();
        }

        var start = options.RangeStart.Value;
        var end = options.RangeEnd!.Value;
        return project.GetChronologicalEvents()
            .Where(timelineEvent => IsInRange(timelineEvent, start, end, options.IncludeOverlappingRanges))
            .ToArray();
    }

    private static bool IsInRange(
        TimelineEvent timelineEvent,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        bool includeOverlappingRanges)
    {
        var eventStart = DateOnly.FromDateTime(timelineEvent.Date.SortStart);
        var eventEnd = timelineEvent.Date.EndYear.HasValue
            ? new DateOnly(
                timelineEvent.Date.EndYear.Value,
                timelineEvent.Date.EndMonth!.Value,
                timelineEvent.Date.EndDay!.Value)
            : eventStart;
        var eventMatches = includeOverlappingRanges
            ? eventEnd >= rangeStart && eventStart <= rangeEnd
            : eventStart >= rangeStart && eventEnd <= rangeEnd;
        var deadlineMatches = timelineEvent.Deadline is { } deadline &&
            deadline.DueDate >= rangeStart && deadline.DueDate <= rangeEnd;
        return eventMatches || deadlineMatches;
    }

    private static (double Width, double Height) ResolvePaperSize(PdfExportOptions options)
    {
        var dimensions = options.PaperSize.Trim().ToUpperInvariant() switch
        {
            "A4" => (Width: 210d, Height: 297d),
            "A3" => (Width: 297d, Height: 420d),
            "LETTER" => (Width: 215.9d, Height: 279.4d),
            "BENUTZERDEFINIERT" or "CUSTOM" =>
                (Width: options.WidthMillimeters, Height: options.HeightMillimeters),
            _ => throw new ArgumentException(
                "Das Papierformat muss A4, A3, Letter oder Benutzerdefiniert sein.",
                nameof(options)),
        };

        var width = dimensions.Width;
        var height = dimensions.Height;
        if (options.Landscape == (height > width))
        {
            (width, height) = (height, width);
        }

        return (width, height);
    }

    private static void ValidateOptions(PdfExportOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PaperSize))
        {
            throw new ArgumentException("Das Papierformat darf nicht leer sein.", nameof(options));
        }

        if (!double.IsFinite(options.FontSize) || options.FontSize is < 8 or > 48)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Die Exportschriftgröße muss zwischen 8 und 48 Punkt liegen.");
        }

        if (options.RangeStart.HasValue != options.RangeEnd.HasValue)
        {
            throw new ArgumentException(
                "Für einen Zeitraumexport müssen Beginn und Ende angegeben werden.",
                nameof(options));
        }

        if (options.RangeStart > options.RangeEnd)
        {
            throw new ArgumentException(
                "Das Ende des Exportzeitraums darf nicht vor dessen Beginn liegen.",
                nameof(options));
        }

        var usesCustomPaper = options.PaperSize.Trim().Equals(
            "Benutzerdefiniert",
            StringComparison.OrdinalIgnoreCase) ||
            options.PaperSize.Trim().Equals("Custom", StringComparison.OrdinalIgnoreCase);
        if (usesCustomPaper &&
            (!double.IsFinite(options.WidthMillimeters) ||
             !double.IsFinite(options.HeightMillimeters) ||
             options.WidthMillimeters is < MinimumPaperMillimeters or > MaximumPaperMillimeters ||
             options.HeightMillimeters is < MinimumPaperMillimeters or > MaximumPaperMillimeters))
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Benutzerdefinierte Papiermaße müssen zwischen 50 und 5.080 mm liegen.");
        }
    }

    private static float ToPoints(double millimeters) => (float)(millimeters * PointsPerMillimeter);

    private static string TranslatePriority(EventPriority priority) => priority switch
    {
        EventPriority.Low => "Niedrig",
        EventPriority.Normal => "Normal",
        EventPriority.High => "Hoch",
        EventPriority.Critical => "Kritisch",
        _ => priority.ToString(),
    };

    private static string TranslateEventStatus(EventStatus status) => status switch
    {
        EventStatus.Active => "Aktiv",
        EventStatus.Completed => "Abgeschlossen",
        EventStatus.Archived => "Archiviert",
        _ => status.ToString(),
    };

    private static string TranslateDeadlineStatus(DeadlineStatus status) => status switch
    {
        DeadlineStatus.Open => "Offen",
        DeadlineStatus.Completed => "Erledigt",
        DeadlineStatus.Cancelled => "Entfallen",
        _ => status.ToString(),
    };

    private sealed record PreparedEvent(
        TimelineEvent Event,
        bool HasThumbnailCandidate,
        IReadOnlyList<PdfTextLine> Lines);
}
