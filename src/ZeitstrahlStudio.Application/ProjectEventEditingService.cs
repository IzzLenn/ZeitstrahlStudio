using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Application;

/// <summary>Erzeugt, ersetzt und entfernt Ereignisse als atomare Aggregatänderungen.</summary>
public sealed class ProjectEventEditingService
{
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
        var existing = project.Events.SingleOrDefault(timelineEvent => timelineEvent.Id == eventId)
            ?? throw new DomainValidationException(
                "Das zu bearbeitende Ereignis wurde nicht gefunden.",
                nameof(eventId));
        var replacement = BuildEvent(
            existing.Id,
            request,
            existing.CreatedAtUtc,
            timestampUtc,
            existing.ManualSortPosition,
            existing.Attachments,
            existing.WebLinks);
        project.ReplaceEvent(replacement, timestampUtc);
        return replacement;
    }

    /// <summary>Entfernt ein vorhandenes Ereignis aus dem Aggregat.</summary>
    public TimelineEvent Delete(
        TimelineProject project,
        Guid eventId,
        DateTimeOffset timestampUtc)
    {
        ArgumentNullException.ThrowIfNull(project);
        return project.RemoveEvent(eventId, timestampUtc);
    }

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
}
