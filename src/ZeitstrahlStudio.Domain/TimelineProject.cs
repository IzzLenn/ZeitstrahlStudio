namespace ZeitstrahlStudio.Domain;

/// <summary>Aggregatwurzel eines lokal gespeicherten Zeitstrahlprojekts.</summary>
public sealed class TimelineProject
{
    private readonly List<TimelineEvent> events = [];
    private readonly List<LayoutPosition> layoutPositions = [];

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
    public IReadOnlyList<LayoutPosition> LayoutPositions => layoutPositions.AsReadOnly();

    /// <summary>Erzeugt ein leeres Projekt.</summary>
    public static TimelineProject Create(Guid id, string name, DateTimeOffset createdAtUtc) =>
        new(id, name, createdAtUtc);

    /// <summary>Stellt ein zuvor validiert gespeichertes Projekt vollständig wieder her.</summary>
    public static TimelineProject Restore(
        Guid id,
        string name,
        string? subtitle,
        string? infoText,
        string? description,
        DateOnly? overallStart,
        DateOnly? overallEnd,
        DateTimeOffset createdAtUtc,
        DateTimeOffset modifiedAtUtc,
        ProjectSettings settings,
        IEnumerable<TimelineEvent> restoredEvents,
        IEnumerable<LayoutPosition> restoredLayoutPositions)
    {
        if (overallStart.HasValue && overallEnd.HasValue && overallEnd < overallStart)
        {
            throw new DomainValidationException("Der gespeicherte Projektzeitraum ist ungültig.", nameof(overallEnd));
        }

        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();

        var project = new TimelineProject(id, name, createdAtUtc)
        {
            Subtitle = NormalizeOptional(subtitle),
            InfoText = NormalizeOptional(infoText),
            Description = NormalizeOptional(description),
            OverallStart = overallStart,
            OverallEnd = overallEnd,
            Settings = settings,
        };
        project.Touch(modifiedAtUtc);

        foreach (var timelineEvent in restoredEvents)
        {
            ArgumentNullException.ThrowIfNull(timelineEvent);
            if (project.events.Any(existing => existing.Id == timelineEvent.Id))
            {
                throw new DomainValidationException("Ein gespeichertes Ereignis ist mehrfach vorhanden.");
            }

            project.events.Add(timelineEvent);
        }

        foreach (var layoutPosition in restoredLayoutPositions)
        {
            ArgumentNullException.ThrowIfNull(layoutPosition);
            if (project.events.All(timelineEvent => timelineEvent.Id != layoutPosition.EventId))
            {
                throw new DomainValidationException("Eine Layoutposition verweist auf ein unbekanntes Ereignis.");
            }

            project.SetLayoutPositionCore(layoutPosition);
        }

        return project;
    }

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

    /// <summary>Ersetzt ein vorhandenes Ereignis atomar durch eine vollständig validierte Fassung.</summary>
    public void ReplaceEvent(TimelineEvent timelineEvent, DateTimeOffset modifiedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(timelineEvent);
        var index = events.FindIndex(existing => existing.Id == timelineEvent.Id);
        if (index < 0)
        {
            throw new DomainValidationException(
                "Das zu ersetzende Ereignis wurde nicht gefunden.",
                nameof(timelineEvent));
        }

        events[index] = timelineEvent;
        Touch(modifiedAtUtc);
    }

    /// <summary>Entfernt ein Ereignis; die Anwendungsschicht protokolliert und kapselt Undo.</summary>
    public TimelineEvent RemoveEvent(Guid eventId, DateTimeOffset modifiedAtUtc)
    {
        var timelineEvent = events.SingleOrDefault(existing => existing.Id == eventId)
            ?? throw new DomainValidationException("Das zu löschende Ereignis wurde nicht gefunden.", nameof(eventId));

        events.Remove(timelineEvent);
        layoutPositions.RemoveAll(position => position.EventId == eventId);
        Touch(modifiedAtUtc);
        return timelineEvent;
    }

    /// <summary>Setzt einen visuellen Versatz für Ereignis und Ausrichtung.</summary>
    public void SetLayoutPosition(LayoutPosition position, DateTimeOffset modifiedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(position);
        if (events.All(timelineEvent => timelineEvent.Id != position.EventId))
        {
            throw new DomainValidationException("Die Layoutposition verweist auf ein unbekanntes Ereignis.", nameof(position));
        }

        SetLayoutPositionCore(position);
        Touch(modifiedAtUtc);
    }

    /// <summary>Entfernt einen einzelnen visuellen Versatz, ohne Ereignisdaten zu verändern.</summary>
    public bool RemoveLayoutPosition(
        Guid eventId,
        TimelineOrientation orientation,
        DateTimeOffset modifiedAtUtc)
    {
        var removed = layoutPositions.RemoveAll(position =>
            position.EventId == eventId && position.Orientation == orientation);
        if (removed == 0)
        {
            return false;
        }

        Touch(modifiedAtUtc);
        return true;
    }

    /// <summary>Entfernt alle manuellen Layoutpositionen und aktiviert wieder die automatische Anordnung.</summary>
    public void ResetLayoutPositions(DateTimeOffset modifiedAtUtc)
    {
        if (layoutPositions.Count == 0)
        {
            return;
        }

        layoutPositions.Clear();
        Touch(modifiedAtUtc);
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

    private void SetLayoutPositionCore(LayoutPosition position)
    {
        layoutPositions.RemoveAll(existing =>
            existing.EventId == position.EventId && existing.Orientation == position.Orientation);
        layoutPositions.Add(position);
    }
}
