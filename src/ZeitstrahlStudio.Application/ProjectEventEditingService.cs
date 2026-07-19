using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Application;

/// <summary>Ergebnis einer Undo-/Redo-Operation.</summary>
public sealed record EventHistoryResult(
    string Operation,
    string Description,
    Guid? SelectedEventId);

/// <summary>
/// Erzeugt, ersetzt, entfernt und sortiert Ereignisse als atomare Aggregatänderungen
/// und hält eine begrenzte projektbezogene Undo-/Redo-Historie.
/// </summary>
public sealed class ProjectEventEditingService
{
    private const int MaximumHistoryEntries = 100;
    private readonly Dictionary<Guid, HistoryState> historyByProject = [];
    private readonly object historyLock = new();

    /// <summary>Erzeugt aus einer vollständigen Eingabe ein neues Projektereignis.</summary>
    public TimelineEvent Create(
        TimelineProject project,
        EventEditRequest request,
        DateTimeOffset timestampUtc)
    {
        ArgumentNullException.ThrowIfNull(project);
        var timelineEvent = BuildEvent(
            Guid.NewGuid(),
            request,
            timestampUtc,
            timestampUtc,
            manualSortPosition: null,
            attachments: [],
            existingWebLinks: []);
        project.AddEvent(timelineEvent, timestampUtc);
        Record(
            project.Id,
            new HistoryEntry(
                $"Ereignis „{timelineEvent.Title}“ erstellt",
                [new EventChange(timelineEvent.Id, Before: null, After: timelineEvent)]));
        return timelineEvent;
    }

    /// <summary>Ersetzt ein vorhandenes Ereignis unter Erhalt technischer IDs und Anhänge.</summary>
    public TimelineEvent Update(
        TimelineProject project,
        Guid eventId,
        EventEditRequest request,
        DateTimeOffset timestampUtc)
    {
        ArgumentNullException.ThrowIfNull(project);
        var existing = FindEvent(project, eventId);
        var replacement = BuildEvent(
            existing.Id,
            request,
            existing.CreatedAtUtc,
            timestampUtc,
            existing.ManualSortPosition,
            existing.Attachments,
            existing.WebLinks);
        project.ReplaceEvent(replacement, timestampUtc);
        Record(
            project.Id,
            new HistoryEntry(
                $"Ereignis „{replacement.Title}“ bearbeitet",
                [new EventChange(eventId, existing, replacement)]));
        return replacement;
    }

    /// <summary>Entfernt ein vorhandenes Ereignis aus dem Aggregat.</summary>
    public TimelineEvent Delete(
        TimelineProject project,
        Guid eventId,
        DateTimeOffset timestampUtc)
    {
        ArgumentNullException.ThrowIfNull(project);
        var removed = project.RemoveEvent(eventId, timestampUtc);
        Record(
            project.Id,
            new HistoryEntry(
                $"Ereignis „{removed.Title}“ gelöscht",
                [new EventChange(eventId, removed, After: null)]));
        return removed;
    }

    /// <summary>Fügt mehrere bereits sicher kopierte Anhänge als einen Undo-Schritt hinzu.</summary>
    public TimelineEvent AddAttachments(
        TimelineProject project,
        Guid eventId,
        IReadOnlyCollection<Attachment> attachments,
        DateTimeOffset timestampUtc)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(attachments);
        if (attachments.Count == 0)
        {
            throw new ArgumentException("Es wurde kein Anhang zum Hinzufügen übergeben.", nameof(attachments));
        }

        var existing = FindEvent(project, eventId);
        var combined = existing.Attachments.Concat(attachments).ToArray();
        if (combined.Select(attachment => attachment.Id).Distinct().Count() != combined.Length)
        {
            throw new DomainValidationException("Ein Anhang ist dem Ereignis bereits zugeordnet.", nameof(attachments));
        }

        var replacement = CloneWithAttachments(existing, combined, timestampUtc);
        project.ReplaceEvent(replacement, timestampUtc);
        Record(
            project.Id,
            new HistoryEntry(
                $"{attachments.Count} Anhang/Anhänge zu „{existing.Title}“ hinzugefügt",
                [new EventChange(eventId, existing, replacement)]));
        return replacement;
    }

    /// <summary>Entfernt eine Anhangszuordnung; die Projektdatei bleibt für Undo erhalten.</summary>
    public TimelineEvent RemoveAttachment(
        TimelineProject project,
        Guid eventId,
        Guid attachmentId,
        DateTimeOffset timestampUtc)
    {
        ArgumentNullException.ThrowIfNull(project);
        var existing = FindEvent(project, eventId);
        var removed = existing.Attachments.SingleOrDefault(attachment => attachment.Id == attachmentId)
            ?? throw new DomainValidationException(
                "Der zu entfernende Anhang wurde nicht gefunden.",
                nameof(attachmentId));
        var retained = existing.Attachments.Where(attachment => attachment.Id != attachmentId).ToArray();
        var replacement = CloneWithAttachments(existing, retained, timestampUtc);
        project.ReplaceEvent(replacement, timestampUtc);
        Record(
            project.Id,
            new HistoryEntry(
                $"Anhang „{removed.OriginalFileName}“ von „{existing.Title}“ entfernt",
                [new EventChange(eventId, existing, replacement)]));
        return replacement;
    }

    /// <summary>Verschiebt ein Ereignis innerhalb einer Gruppe mit identischem Datum.</summary>
    public bool MoveWithinSameDate(
        TimelineProject project,
        Guid eventId,
        bool moveEarlier,
        DateTimeOffset timestampUtc)
    {
        ArgumentNullException.ThrowIfNull(project);
        var selected = FindEvent(project, eventId);
        var group = project.GetChronologicalEvents()
            .Where(timelineEvent => timelineEvent.Date == selected.Date)
            .ToList();
        var currentIndex = group.FindIndex(timelineEvent => timelineEvent.Id == eventId);
        var targetIndex = currentIndex + (moveEarlier ? -1 : 1);
        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= group.Count)
        {
            return false;
        }

        (group[currentIndex], group[targetIndex]) = (group[targetIndex], group[currentIndex]);
        var changes = new List<EventChange>(group.Count);
        for (var index = 0; index < group.Count; index++)
        {
            var current = FindEvent(project, group[index].Id);
            var replacement = CloneWithManualSortPosition(
                current,
                (index + 1) * 10m,
                timestampUtc);
            project.ReplaceEvent(replacement, timestampUtc);
            changes.Add(new EventChange(current.Id, current, replacement));
        }

        Record(
            project.Id,
            new HistoryEntry(
                $"Reihenfolge von „{selected.Title}“ geändert",
                changes));
        return true;
    }

    public bool CanMoveWithinSameDate(
        TimelineProject project,
        Guid eventId,
        bool moveEarlier)
    {
        ArgumentNullException.ThrowIfNull(project);
        var selected = FindEvent(project, eventId);
        var group = project.GetChronologicalEvents()
            .Where(timelineEvent => timelineEvent.Date == selected.Date)
            .ToList();
        var currentIndex = group.FindIndex(timelineEvent => timelineEvent.Id == eventId);
        var targetIndex = currentIndex + (moveEarlier ? -1 : 1);
        return currentIndex >= 0 && targetIndex >= 0 && targetIndex < group.Count;
    }

    public bool CanUndo(Guid projectId)
    {
        lock (historyLock)
        {
            return historyByProject.TryGetValue(projectId, out var state) && state.Undo.Count > 0;
        }
    }

    public bool CanRedo(Guid projectId)
    {
        lock (historyLock)
        {
            return historyByProject.TryGetValue(projectId, out var state) && state.Redo.Count > 0;
        }
    }

    /// <summary>Macht die letzte Ereignisoperation des Projekts rückgängig.</summary>
    public EventHistoryResult Undo(TimelineProject project, DateTimeOffset timestampUtc)
    {
        ArgumentNullException.ThrowIfNull(project);
        lock (historyLock)
        {
            var state = GetState(project.Id);
            if (state.Undo.Count == 0)
            {
                throw new InvalidOperationException("Es ist keine Ereignisänderung zum Rückgängigmachen vorhanden.");
            }

            var entry = state.Undo[^1];
            Apply(project, entry, useBeforeState: true, timestampUtc);
            state.Undo.RemoveAt(state.Undo.Count - 1);
            state.Redo.Add(entry);
            return new EventHistoryResult(
                "Undo",
                $"Rückgängig: {entry.Description}",
                GetSelectedEventId(entry, useBeforeState: true));
        }
    }

    /// <summary>Stellt die zuletzt rückgängig gemachte Ereignisoperation wieder her.</summary>
    public EventHistoryResult Redo(TimelineProject project, DateTimeOffset timestampUtc)
    {
        ArgumentNullException.ThrowIfNull(project);
        lock (historyLock)
        {
            var state = GetState(project.Id);
            if (state.Redo.Count == 0)
            {
                throw new InvalidOperationException("Es ist keine Ereignisänderung zum Wiederholen vorhanden.");
            }

            var entry = state.Redo[^1];
            Apply(project, entry, useBeforeState: false, timestampUtc);
            state.Redo.RemoveAt(state.Redo.Count - 1);
            state.Undo.Add(entry);
            return new EventHistoryResult(
                "Redo",
                $"Wiederholt: {entry.Description}",
                GetSelectedEventId(entry, useBeforeState: false));
        }
    }

    /// <summary>Verwirft die Historie eines geschlossenen Projekts.</summary>
    public void Clear(Guid projectId)
    {
        lock (historyLock)
        {
            historyByProject.Remove(projectId);
        }
    }

    private static TimelineEvent FindEvent(TimelineProject project, Guid eventId) =>
        project.Events.SingleOrDefault(timelineEvent => timelineEvent.Id == eventId)
        ?? throw new DomainValidationException(
            "Das Ereignis wurde nicht gefunden.",
            nameof(eventId));

    private static void Apply(
        TimelineProject project,
        HistoryEntry entry,
        bool useBeforeState,
        DateTimeOffset timestampUtc)
    {
        foreach (var change in entry.Changes)
        {
            var target = useBeforeState ? change.Before : change.After;
            var existing = project.Events.SingleOrDefault(
                timelineEvent => timelineEvent.Id == change.EventId);
            if (target is null && existing is not null)
            {
                project.RemoveEvent(change.EventId, timestampUtc);
            }
            else if (target is not null && existing is null)
            {
                project.AddEvent(target, timestampUtc);
            }
            else if (target is not null)
            {
                project.ReplaceEvent(target, timestampUtc);
            }
        }
    }

    private static Guid? GetSelectedEventId(HistoryEntry entry, bool useBeforeState) =>
        entry.Changes
            .Select(change => useBeforeState ? change.Before : change.After)
            .FirstOrDefault(timelineEvent => timelineEvent is not null)
            ?.Id;

    private void Record(Guid projectId, HistoryEntry entry)
    {
        lock (historyLock)
        {
            var state = GetState(projectId);
            state.Undo.Add(entry);
            if (state.Undo.Count > MaximumHistoryEntries)
            {
                state.Undo.RemoveAt(0);
            }

            state.Redo.Clear();
        }
    }

    private HistoryState GetState(Guid projectId)
    {
        if (!historyByProject.TryGetValue(projectId, out var state))
        {
            state = new HistoryState();
            historyByProject.Add(projectId, state);
        }

        return state;
    }

    private static TimelineEvent CloneWithManualSortPosition(
        TimelineEvent source,
        decimal? manualSortPosition,
        DateTimeOffset modifiedAtUtc) =>
        TimelineEvent.Restore(
            source.Id,
            source.Date,
            source.Title,
            source.InfoText,
            source.Description,
            source.Deadline,
            source.Priority,
            source.ColorHex,
            source.Source,
            source.Notes,
            source.Status,
            manualSortPosition,
            source.CreatedAtUtc,
            modifiedAtUtc,
            source.Tags,
            source.Attachments,
            source.WebLinks);

    private static TimelineEvent CloneWithAttachments(
        TimelineEvent source,
        IEnumerable<Attachment> attachments,
        DateTimeOffset modifiedAtUtc) =>
        TimelineEvent.Restore(
            source.Id,
            source.Date,
            source.Title,
            source.InfoText,
            source.Description,
            source.Deadline,
            source.Priority,
            source.ColorHex,
            source.Source,
            source.Notes,
            source.Status,
            source.ManualSortPosition,
            source.CreatedAtUtc,
            modifiedAtUtc,
            source.Tags,
            attachments,
            source.WebLinks);

    private static TimelineEvent BuildEvent(
        Guid eventId,
        EventEditRequest request,
        DateTimeOffset createdAtUtc,
        DateTimeOffset modifiedAtUtc,
        decimal? manualSortPosition,
        IEnumerable<Attachment> attachments,
        IEnumerable<WebLink> existingWebLinks)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Date);
        ArgumentNullException.ThrowIfNull(request.Tags);
        ArgumentNullException.ThrowIfNull(request.WebLinks);

        var previousLinks = existingWebLinks.ToArray();
        var links = request.WebLinks.Select(input =>
        {
            if (string.IsNullOrWhiteSpace(input.Address) ||
                !Uri.TryCreate(input.Address.Trim(), UriKind.Absolute, out var address))
            {
                throw new DomainValidationException(
                    "Eine Webseitenadresse ist ungültig.",
                    nameof(request));
            }

            var preservedId = input.Id ??
                previousLinks.FirstOrDefault(link => link.Address == address)?.Id ??
                Guid.NewGuid();
            return new WebLink(preservedId, address, input.Label);
        }).ToArray();

        return TimelineEvent.Restore(
            eventId,
            request.Date,
            request.Title,
            request.InfoText,
            request.Description,
            request.Deadline,
            request.Priority,
            request.ColorHex,
            request.Source,
            request.Notes,
            request.Status,
            manualSortPosition,
            createdAtUtc,
            modifiedAtUtc,
            request.Tags,
            attachments,
            links);
    }

    private sealed record EventChange(
        Guid EventId,
        TimelineEvent? Before,
        TimelineEvent? After);

    private sealed record HistoryEntry(
        string Description,
        IReadOnlyList<EventChange> Changes);

    private sealed class HistoryState
    {
        public List<HistoryEntry> Undo { get; } = [];
        public List<HistoryEntry> Redo { get; } = [];
    }
}
