using System.Globalization;
using System.Text.RegularExpressions;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Bindbares Eingabemodell für die vollständige Ereignisbearbeitung.</summary>
public sealed partial class EventEditorDialogViewModel : ObservableObject
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");
    private readonly TimelineEvent? existingEvent;
    private DatePrecision selectedPrecision = DatePrecision.ExactDate;
    private string title = string.Empty;
    private string startYear = DateTime.Today.Year.ToString(CultureInfo.InvariantCulture);
    private string startMonth = DateTime.Today.Month.ToString(CultureInfo.InvariantCulture);
    private string startDay = DateTime.Today.Day.ToString(CultureInfo.InvariantCulture);
    private string startTime = string.Empty;
    private string endYear = DateTime.Today.Year.ToString(CultureInfo.InvariantCulture);
    private string endMonth = DateTime.Today.Month.ToString(CultureInfo.InvariantCulture);
    private string endDay = DateTime.Today.Day.ToString(CultureInfo.InvariantCulture);
    private string? infoText;
    private string? description;
    private string? source;
    private string? notes;
    private EventPriority selectedPriority = EventPriority.Normal;
    private EventStatus selectedStatus = EventStatus.Active;
    private string colorHex = "#3B82F6";
    private string tagsText = string.Empty;
    private string webLinksText = string.Empty;
    private bool hasDeadline;
    private DateTime? deadlineDate;
    private string deadlineTime = string.Empty;
    private string? deadlineLabel;
    private DeadlineStatus selectedDeadlineStatus = DeadlineStatus.Open;
    private string? reminderNote;

    public EventEditorDialogViewModel(
        TimelineEvent? timelineEvent,
        string defaultEventColorHex = "#3B82F6")
    {
        existingEvent = timelineEvent;
        var defaultSettings = new ProjectSettings { DefaultEventColorHex = defaultEventColorHex };
        defaultSettings.Validate();
        colorHex = defaultSettings.DefaultEventColorHex;
        DatePrecisionOptions =
        [
            new(DatePrecision.ExactDate, "Exaktes Datum"),
            new(DatePrecision.ExactDateTime, "Datum und Uhrzeit"),
            new(DatePrecision.MonthAndYear, "Monat und Jahr"),
            new(DatePrecision.Year, "Nur Jahr"),
            new(DatePrecision.DateRange, "Zeitraum"),
        ];
        PriorityOptions =
        [
            new(EventPriority.Low, "Niedrig"),
            new(EventPriority.Normal, "Normal"),
            new(EventPriority.High, "Hoch"),
            new(EventPriority.Critical, "Kritisch"),
        ];
        StatusOptions =
        [
            new(EventStatus.Active, "Aktiv"),
            new(EventStatus.Completed, "Abgeschlossen"),
            new(EventStatus.Archived, "Archiviert"),
        ];
        DeadlineStatusOptions =
        [
            new(DeadlineStatus.Open, "Offen"),
            new(DeadlineStatus.Completed, "Erledigt"),
            new(DeadlineStatus.Cancelled, "Entfallen"),
        ];

        if (timelineEvent is not null)
        {
            Load(timelineEvent);
        }
    }

    public string DialogTitle => existingEvent is null ? "Ereignis erstellen" : "Ereignis bearbeiten";
    public IReadOnlyList<SelectionOption<DatePrecision>> DatePrecisionOptions { get; }
    public IReadOnlyList<SelectionOption<EventPriority>> PriorityOptions { get; }
    public IReadOnlyList<SelectionOption<EventStatus>> StatusOptions { get; }
    public IReadOnlyList<SelectionOption<DeadlineStatus>> DeadlineStatusOptions { get; }
    public IReadOnlyList<ColorPaletteOption> ColorOptions => EventColorPalette.Options;

    public string Title
    {
        get => title;
        set => SetProperty(ref title, value);
    }

    public DatePrecision SelectedPrecision
    {
        get => selectedPrecision;
        set
        {
            if (SetProperty(ref selectedPrecision, value))
            {
                OnPropertyChanged(nameof(NeedsMonth));
                OnPropertyChanged(nameof(NeedsDay));
                OnPropertyChanged(nameof(NeedsTime));
                OnPropertyChanged(nameof(NeedsEndDate));
            }
        }
    }

    public bool NeedsMonth => SelectedPrecision is not DatePrecision.Year;
    public bool NeedsDay => SelectedPrecision is DatePrecision.ExactDate
        or DatePrecision.ExactDateTime
        or DatePrecision.DateRange;
    public bool NeedsTime => SelectedPrecision == DatePrecision.ExactDateTime;
    public bool NeedsEndDate => SelectedPrecision == DatePrecision.DateRange;

    public string StartYear
    {
        get => startYear;
        set => SetProperty(ref startYear, value);
    }

    public string StartMonth
    {
        get => startMonth;
        set => SetProperty(ref startMonth, value);
    }

    public string StartDay
    {
        get => startDay;
        set => SetProperty(ref startDay, value);
    }

    public string StartTime
    {
        get => startTime;
        set => SetProperty(ref startTime, value);
    }

    public string EndYear
    {
        get => endYear;
        set => SetProperty(ref endYear, value);
    }

    public string EndMonth
    {
        get => endMonth;
        set => SetProperty(ref endMonth, value);
    }

    public string EndDay
    {
        get => endDay;
        set => SetProperty(ref endDay, value);
    }

    public string? InfoText
    {
        get => infoText;
        set => SetProperty(ref infoText, value);
    }

    public string? Description
    {
        get => description;
        set => SetProperty(ref description, value);
    }

    public string? Source
    {
        get => source;
        set => SetProperty(ref source, value);
    }

    public string? Notes
    {
        get => notes;
        set => SetProperty(ref notes, value);
    }

    public EventPriority SelectedPriority
    {
        get => selectedPriority;
        set => SetProperty(ref selectedPriority, value);
    }

    public EventStatus SelectedStatus
    {
        get => selectedStatus;
        set => SetProperty(ref selectedStatus, value);
    }

    public string ColorHex
    {
        get => colorHex;
        set
        {
            if (SetProperty(ref colorHex, value))
            {
                OnPropertyChanged(nameof(SelectedPaletteColorHex));
            }
        }
    }

    public string? SelectedPaletteColorHex
    {
        get => ColorOptions.FirstOrDefault(option =>
            string.Equals(option.Hex, ColorHex, StringComparison.OrdinalIgnoreCase))?.Hex;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                ColorHex = value;
            }
        }
    }

    public string TagsText
    {
        get => tagsText;
        set => SetProperty(ref tagsText, value);
    }

    public string WebLinksText
    {
        get => webLinksText;
        set => SetProperty(ref webLinksText, value);
    }

    public bool HasDeadline
    {
        get => hasDeadline;
        set => SetProperty(ref hasDeadline, value);
    }

    public DateTime? DeadlineDate
    {
        get => deadlineDate;
        set => SetProperty(ref deadlineDate, value);
    }

    public string DeadlineTime
    {
        get => deadlineTime;
        set => SetProperty(ref deadlineTime, value);
    }

    public string? DeadlineLabel
    {
        get => deadlineLabel;
        set => SetProperty(ref deadlineLabel, value);
    }

    public DeadlineStatus SelectedDeadlineStatus
    {
        get => selectedDeadlineStatus;
        set => SetProperty(ref selectedDeadlineStatus, value);
    }

    public string? ReminderNote
    {
        get => reminderNote;
        set => SetProperty(ref reminderNote, value);
    }

    public bool TryBuildRequest(out EventEditRequest? request, out string errorMessage)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Title))
            {
                throw new DomainValidationException("Bitte geben Sie einen Ereignistitel ein.");
            }

            var normalizedColor = ColorHex?.Trim() ?? string.Empty;
            if (!ColorPattern().IsMatch(normalizedColor))
            {
                throw new DomainValidationException("Die Farbe muss im Format #RRGGBB angegeben werden.");
            }

            request = new EventEditRequest(
                BuildEventDate(),
                Title,
                InfoText,
                Description,
                BuildDeadline(),
                SelectedPriority,
                normalizedColor,
                Source,
                Notes,
                SelectedStatus,
                ParseTags(),
                ParseWebLinks());
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception exception) when (
            exception is DomainValidationException or ArgumentOutOfRangeException or FormatException)
        {
            request = null;
            errorMessage = exception.Message;
            return false;
        }
    }

    private void Load(TimelineEvent timelineEvent)
    {
        Title = timelineEvent.Title;
        SelectedPrecision = timelineEvent.Date.Precision;
        StartYear = timelineEvent.Date.StartYear.ToString(CultureInfo.InvariantCulture);
        StartMonth = timelineEvent.Date.StartMonth?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        StartDay = timelineEvent.Date.StartDay?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        StartTime = timelineEvent.Date.StartTime?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? string.Empty;
        EndYear = timelineEvent.Date.EndYear?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        EndMonth = timelineEvent.Date.EndMonth?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        EndDay = timelineEvent.Date.EndDay?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        InfoText = timelineEvent.InfoText;
        Description = timelineEvent.Description;
        Source = timelineEvent.Source;
        Notes = timelineEvent.Notes;
        SelectedPriority = timelineEvent.Priority;
        SelectedStatus = timelineEvent.Status;
        ColorHex = timelineEvent.ColorHex;
        TagsText = string.Join(", ", timelineEvent.Tags.Order(StringComparer.CurrentCultureIgnoreCase));
        WebLinksText = string.Join(
            Environment.NewLine,
            timelineEvent.WebLinks.Select(link =>
                string.IsNullOrWhiteSpace(link.Label)
                    ? link.Address.AbsoluteUri
                    : $"{link.Label} | {link.Address.AbsoluteUri}"));

        if (timelineEvent.Deadline is { } deadline)
        {
            HasDeadline = true;
            DeadlineDate = deadline.DueDate.ToDateTime(TimeOnly.MinValue);
            DeadlineTime = deadline.DueTime?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? string.Empty;
            DeadlineLabel = deadline.Label;
            SelectedDeadlineStatus = deadline.Status;
            ReminderNote = deadline.ReminderNote;
        }
    }

    private EventDate BuildEventDate()
    {
        var year = ParseNumber(StartYear, "Startjahr");
        return SelectedPrecision switch
        {
            DatePrecision.Year => EventDate.Year(year),
            DatePrecision.MonthAndYear => EventDate.MonthAndYear(
                year,
                ParseNumber(StartMonth, "Startmonat")),
            DatePrecision.ExactDate => EventDate.Exact(BuildStartDate(year)),
            DatePrecision.ExactDateTime => EventDate.ExactWithTime(
                BuildStartDate(year),
                ParseTime(StartTime, "Uhrzeit", required: true)!.Value),
            DatePrecision.DateRange => EventDate.Range(
                BuildStartDate(year),
                new DateOnly(
                    ParseNumber(EndYear, "Endjahr"),
                    ParseNumber(EndMonth, "Endmonat"),
                    ParseNumber(EndDay, "Endtag"))),
            _ => throw new DomainValidationException("Die Datumsgenauigkeit wird nicht unterstützt."),
        };
    }

    private DateOnly BuildStartDate(int year) => new(
        year,
        ParseNumber(StartMonth, "Startmonat"),
        ParseNumber(StartDay, "Starttag"));

    private Deadline? BuildDeadline()
    {
        if (!HasDeadline)
        {
            return null;
        }

        if (DeadlineDate is null)
        {
            throw new DomainValidationException("Bitte wählen Sie ein Fälligkeitsdatum.");
        }

        var existingId = existingEvent?.Deadline?.Id ?? Guid.NewGuid();
        return new Deadline(
            existingId,
            DateOnly.FromDateTime(DeadlineDate.Value),
            ParseTime(DeadlineTime, "Fristuhrzeit", required: false),
            DeadlineLabel,
            SelectedDeadlineStatus,
            ReminderNote);
    }

    private IReadOnlyList<string> ParseTags() => TagsText
        .Split([',', ';', (char)13, (char)10], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Distinct(StringComparer.CurrentCultureIgnoreCase)
        .ToArray();

    private IReadOnlyList<WebLinkInput> ParseWebLinks()
    {
        var result = new List<WebLinkInput>();
        var addresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in WebLinksText.Split(
                     [(char)13, (char)10],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = line.IndexOf('|');
            var label = separator >= 0 ? line[..separator].Trim() : null;
            var addressText = (separator >= 0 ? line[(separator + 1)..] : line).Trim();
            if (!Uri.TryCreate(addressText, UriKind.Absolute, out var address) ||
                (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps))
            {
                throw new DomainValidationException(
                    $"Die Webseitenadresse „{addressText}“ ist keine gültige HTTP-/HTTPS-Adresse.");
            }

            if (!addresses.Add(address.AbsoluteUri))
            {
                throw new DomainValidationException(
                    $"Die Webseitenadresse „{address.AbsoluteUri}“ ist mehrfach vorhanden.");
            }

            var existingId = existingEvent?.WebLinks
                .FirstOrDefault(link => link.Address == address)?.Id;
            result.Add(new WebLinkInput(existingId, address.AbsoluteUri, label));
        }

        return result;
    }

    private static int ParseNumber(string value, string fieldName)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result))
        {
            throw new DomainValidationException($"{fieldName} muss eine ganze Zahl sein.");
        }

        return result;
    }

    private static TimeOnly? ParseTime(string value, string fieldName, bool required)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
            {
                throw new DomainValidationException($"{fieldName} darf nicht leer sein.");
            }

            return null;
        }

        if (!TimeOnly.TryParse(value.Trim(), GermanCulture, DateTimeStyles.None, out var time))
        {
            throw new DomainValidationException($"{fieldName} muss im Format HH:mm angegeben werden.");
        }

        return time;
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorPattern();
}

/// <summary>Lokalisierte Auswahloption für einen Enum-Wert.</summary>
public sealed record SelectionOption<T>(T Value, string Label)
    where T : struct, Enum;
