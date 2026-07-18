namespace ZeitstrahlStudio.Domain;

/// <summary>Eine vom Ereignisdatum unabhängige Frist.</summary>
public sealed record Deadline
{
    /// <summary>Initialisiert eine Frist.</summary>
    public Deadline(
        Guid id,
        DateOnly dueDate,
        TimeOnly? dueTime = null,
        string? label = null,
        DeadlineStatus status = DeadlineStatus.Open,
        string? reminderNote = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Eine Frist benötigt eine gültige ID.", nameof(id));
        }

        Id = id;
        DueDate = dueDate;
        DueTime = dueTime;
        Label = NormalizeOptional(label);
        Status = status;
        ReminderNote = NormalizeOptional(reminderNote);
    }

    /// <summary>Eindeutige ID.</summary>
    public Guid Id { get; }

    /// <summary>Fälligkeitsdatum.</summary>
    public DateOnly DueDate { get; }

    /// <summary>Optionale Fälligkeitszeit.</summary>
    public TimeOnly? DueTime { get; }

    /// <summary>Optionale Bezeichnung.</summary>
    public string? Label { get; }

    /// <summary>Bearbeitungszustand.</summary>
    public DeadlineStatus Status { get; }

    /// <summary>Optionale Erinnerungsnotiz.</summary>
    public string? ReminderNote { get; }

    /// <summary>Bestimmt die visuelle Dringlichkeit relativ zur lokalen Zeit.</summary>
    public DeadlineUrgency GetUrgency(DateTime localNow, TimeSpan upcomingWindow)
    {
        if (upcomingWindow < TimeSpan.Zero)
        {
            throw new DomainValidationException(
                "Das Zeitfenster für bevorstehende Fristen darf nicht negativ sein.",
                nameof(upcomingWindow));
        }

        if (Status == DeadlineStatus.Completed)
        {
            return DeadlineUrgency.Completed;
        }

        if (Status == DeadlineStatus.Cancelled)
        {
            return DeadlineUrgency.None;
        }

        var dueAt = DueDate.ToDateTime(DueTime ?? TimeOnly.MaxValue);
        if (dueAt < localNow)
        {
            return DeadlineUrgency.Overdue;
        }

        return dueAt <= localNow.Add(upcomingWindow)
            ? DeadlineUrgency.Upcoming
            : DeadlineUrgency.Open;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
