namespace ZeitstrahlStudio.Domain;

/// <summary>Zentraler fachlicher Zeitstrahleintrag.</summary>
public sealed class TimelineEvent
{
    private readonly List<Attachment> attachments = [];
    private readonly HashSet<string> tags = new(StringComparer.CurrentCultureIgnoreCase);
    private readonly List<WebLink> webLinks = [];

    private TimelineEvent(Guid id, string title, EventDate date, DateTimeOffset createdAtUtc)
    {
        ValidateId(id);
        ValidateUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        Title = NormalizeRequired(title, "Der Ereignistitel darf nicht leer sein.", nameof(title));
        Date = date ?? throw new ArgumentNullException(nameof(date));
        CreatedAtUtc = createdAtUtc;
        ModifiedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }
    public EventDate Date { get; private set; }
    public string Title { get; private set; }
    public string? InfoText { get; private set; }
    public string? Description { get; private set; }
    public Deadline? Deadline { get; private set; }
    public EventPriority Priority { get; private set; } = EventPriority.Normal;
    public string ColorHex { get; private set; } = "#3B82F6";
    public string? Source { get; private set; }
    public string? Notes { get; private set; }
    public EventStatus Status { get; private set; } = EventStatus.Active;
    public decimal? ManualSortPosition { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ModifiedAtUtc { get; private set; }
    public IReadOnlyCollection<Attachment> Attachments => attachments.AsReadOnly();
    public IReadOnlyCollection<string> Tags => tags;
    public IReadOnlyCollection<WebLink> WebLinks => webLinks.AsReadOnly();

    /// <summary>Erzeugt ein gültiges Ereignis.</summary>
    public static TimelineEvent Create(Guid id, string title, EventDate date, DateTimeOffset createdAtUtc) =>
        new(id, title, date, createdAtUtc);

    /// <summary>Stellt ein zuvor validiert gespeichertes Ereignis vollständig wieder her.</summary>
    public static TimelineEvent Restore(
        Guid id,
        EventDate date,
        string title,
        string? infoText,
        string? description,
        Deadline? deadline,
        EventPriority priority,
        string colorHex,
        string? source,
        string? notes,
        EventStatus status,
        decimal? manualSortPosition,
        DateTimeOffset createdAtUtc,
        DateTimeOffset modifiedAtUtc,
        IEnumerable<string> restoredTags,
        IEnumerable<Attachment> restoredAttachments,
        IEnumerable<WebLink> restoredWebLinks)
    {
        var timelineEvent = new TimelineEvent(id, title, date, createdAtUtc)
        {
            InfoText = NormalizeOptional(infoText),
            Description = NormalizeOptional(description),
            Deadline = deadline,
            Priority = priority,
            ColorHex = NormalizeColor(colorHex),
            Source = NormalizeOptional(source),
            Notes = NormalizeOptional(notes),
            Status = status,
            ManualSortPosition = manualSortPosition,
        };

        timelineEvent.Touch(modifiedAtUtc);

        foreach (var tag in restoredTags)
        {
            timelineEvent.tags.Add(NormalizeRequired(tag, "Ein Schlagwort darf nicht leer sein.", nameof(restoredTags)));
        }

        foreach (var attachment in restoredAttachments)
        {
            ArgumentNullException.ThrowIfNull(attachment);
            if (timelineEvent.attachments.Any(existing => existing.Id == attachment.Id))
            {
                throw new DomainValidationException("Ein gespeicherter Anhang ist mehrfach vorhanden.");
            }

            timelineEvent.attachments.Add(attachment);
        }

        foreach (var webLink in restoredWebLinks)
        {
            ArgumentNullException.ThrowIfNull(webLink);
            if (timelineEvent.webLinks.Any(existing => existing.Id == webLink.Id || existing.Address == webLink.Address))
            {
                throw new DomainValidationException("Ein gespeicherter Webseitenlink ist mehrfach vorhanden.");
            }

            timelineEvent.webLinks.Add(webLink);
        }

        return timelineEvent;
    }

    /// <summary>Ändert die frei formulierbaren Inhalte.</summary>
    public void UpdateContent(
        string title,
        string? infoText,
        string? description,
        string? source,
        string? notes,
        DateTimeOffset modifiedAtUtc)
    {
        Title = NormalizeRequired(title, "Der Ereignistitel darf nicht leer sein.", nameof(title));
        InfoText = NormalizeOptional(infoText);
        Description = NormalizeOptional(description);
        Source = NormalizeOptional(source);
        Notes = NormalizeOptional(notes);
        Touch(modifiedAtUtc);
    }

    /// <summary>Ändert die Datumsangabe unter Erhalt ihrer Genauigkeit.</summary>
    public void ChangeDate(EventDate date, DateTimeOffset modifiedAtUtc)
    {
        Date = date ?? throw new ArgumentNullException(nameof(date));
        Touch(modifiedAtUtc);
    }

    /// <summary>Setzt oder entfernt die unabhängige Frist.</summary>
    public void SetDeadline(Deadline? deadline, DateTimeOffset modifiedAtUtc)
    {
        Deadline = deadline;
        Touch(modifiedAtUtc);
    }

    /// <summary>Ändert Darstellungsmerkmale und Status.</summary>
    public void SetClassification(
        EventPriority priority,
        EventStatus status,
        string colorHex,
        DateTimeOffset modifiedAtUtc)
    {
        Priority = priority;
        Status = status;
        ColorHex = NormalizeColor(colorHex);
        Touch(modifiedAtUtc);
    }

    /// <summary>Setzt die manuelle Reihenfolge gleicher Datumswerte.</summary>
    public void SetManualSortPosition(decimal? position, DateTimeOffset modifiedAtUtc)
    {
        ManualSortPosition = position;
        Touch(modifiedAtUtc);
    }

    /// <summary>Fügt ein Schlagwort hinzu.</summary>
    public bool AddTag(string tag, DateTimeOffset modifiedAtUtc)
    {
        var normalized = NormalizeRequired(tag, "Ein Schlagwort darf nicht leer sein.", nameof(tag));
        var added = tags.Add(normalized);
        if (added)
        {
            Touch(modifiedAtUtc);
        }

        return added;
    }

    /// <summary>Entfernt ein Schlagwort.</summary>
    public bool RemoveTag(string tag, DateTimeOffset modifiedAtUtc)
    {
        var removed = tags.Remove(tag);
        if (removed)
        {
            Touch(modifiedAtUtc);
        }

        return removed;
    }

    /// <summary>Verknüpft einen importierten Anhang.</summary>
    public void AddAttachment(Attachment attachment, DateTimeOffset modifiedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        if (attachments.Any(existing => existing.Id == attachment.Id))
        {
            throw new DomainValidationException("Der Anhang ist dem Ereignis bereits zugeordnet.", nameof(attachment));
        }

        attachments.Add(attachment);
        Touch(modifiedAtUtc);
    }

    /// <summary>Entfernt die Zuordnung eines Anhangs.</summary>
    public bool RemoveAttachment(Guid attachmentId, DateTimeOffset modifiedAtUtc)
    {
        var removed = attachments.RemoveAll(attachment => attachment.Id == attachmentId) > 0;
        if (removed)
        {
            Touch(modifiedAtUtc);
        }

        return removed;
    }

    /// <summary>Fügt einen Webseitenlink hinzu.</summary>
    public void AddWebLink(WebLink webLink, DateTimeOffset modifiedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(webLink);
        if (webLinks.Any(existing => existing.Id == webLink.Id || existing.Address == webLink.Address))
        {
            throw new DomainValidationException("Der Webseitenlink ist bereits zugeordnet.", nameof(webLink));
        }

        webLinks.Add(webLink);
        Touch(modifiedAtUtc);
    }

    private static string NormalizeColor(string value)
    {
        if (value.Length != 7 || value[0] != '#' || !value.AsSpan(1).ToArray().All(Uri.IsHexDigit))
        {
            throw new DomainValidationException("Die Farbe muss als #RRGGBB angegeben werden.", nameof(value));
        }

        return value.ToUpperInvariant();
    }

    private static string NormalizeRequired(string value, string message, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException(message, parameterName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateId(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Ein Ereignis benötigt eine gültige ID.", nameof(id));
        }
    }

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
