namespace ZeitstrahlStudio.Domain;

/// <summary>Aggregatwurzel eines lokal gespeicherten Zeitstrahlprojekts.</summary>
public sealed class TimelineProject
{
    private readonly List<TimelineEvent> events = [];

    private TimelineProject(Guid id, string name, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Ein Projekt benötigt eine gültige ID.", nameof(id));
        }

        ValidateUtc(createdAtUtc, nameof(createdAtUtc));
        Id = id;
        Name = NormalizeRequired(name);
        CreatedAtUtc = createdAtUtc;
        ModifiedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }
    public string Name { get; private set; }
    public string? Subtitle { get; private set; }
    public string? InfoText { get; private set; }
    public string? Description { get; private set; }
    public DateOnly? OverallStart { get; private set; }
    public DateOnly? OverallEnd { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ModifiedAtUtc { get; private set; }
    public ProjectSettings Settings { get; private set; } = new();
    public IReadOnlyList<TimelineEvent> Events => events.AsReadOnly();

    /// <summary>Erzeugt ein leeres Projekt.</summary>
    public static TimelineProject Create(Guid id, string name, DateTimeOffset createdAtUtc) =>
        new(id, name, createdAtUtc);

    /// <summary>Ändert die Projektinformationen.</summary>
    public void UpdateInformation(
        string name,
        string? subtitle,
        string? infoText,
        string? description,
        DateOnly? overallStart,
        DateOnly? overallEnd,
        DateTimeOffset modifiedAtUtc)
    {
        if (overallStart.HasValue && overallEnd.HasValue && overallEnd < overallStart)
        {
            throw new DomainValidationException(
                "Das Ende des Projektzeitraums darf nicht vor dessen Beginn liegen.",
                nameof(overallEnd));
        }

        Name = NormalizeRequired(name);
        Subtitle = NormalizeOptional(subtitle);
        InfoText = NormalizeOptional(infoText);
        Description = NormalizeOptional(description);
        OverallStart = overallStart;
        OverallEnd = overallEnd;
        Touch(modifiedAtUtc);
    }

    /// <summary>Ersetzt die validierten Projekteinstellungen.</summary>
    public void ChangeSettings(ProjectSettings settings, DateTimeOffset modifiedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        Settings = settings;
        Touch(modifiedAtUtc);
    }

    /// <summary>Fügt ein Ereignis zum Projekt hinzu.</summary>
    public void AddEvent(TimelineEvent timelineEvent, DateTimeOffset modifiedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(timelineEvent);
        if (events.Any(existing => existing.Id == timelineEvent.Id))
        {
            throw new DomainValidationException("Ein Ereignis mit dieser ID ist bereits vorhanden.", nameof(timelineEvent));
        }

        events.Add(timelineEvent);
        Touch(modifiedAtUtc);
    }

    /// <summary>Entfernt ein Ereignis; die Anwendungsschicht protokolliert und kapselt Undo.</summary>
    public TimelineEvent RemoveEvent(Guid eventId, DateTimeOffset modifiedAtUtc)
    {
        var timelineEvent = events.SingleOrDefault(existing => existing.Id == eventId)
            ?? throw new DomainValidationException("Das zu löschende Ereignis wurde nicht gefunden.", nameof(eventId));

        events.Remove(timelineEvent);
        Touch(modifiedAtUtc);
        return timelineEvent;
    }

    /// <summary>Gibt Ereignisse in stabiler chronologischer Reihenfolge zurück.</summary>
    public IReadOnlyList<TimelineEvent> GetChronologicalEvents() =>
        events.OrderBy(item => item, TimelineEventComparer.Instance).ToArray();

    private static string NormalizeRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("Der Projektname darf nicht leer sein.", nameof(value));
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainValidationException("Technische Zeitstempel müssen in UTC gespeichert werden.", parameterName);
        }
    }

    private void Touch(DateTimeOffset modifiedAtUtc)
    {
        ValidateUtc(modifiedAtUtc, nameof(modifiedAtUtc));
        if (modifiedAtUtc < CreatedAtUtc)
        {
            throw new DomainValidationException(
                "Der Änderungszeitpunkt darf nicht vor dem Erstellungszeitpunkt liegen.",
                nameof(modifiedAtUtc));
        }

        ModifiedAtUtc = modifiedAtUtc;
    }
}
