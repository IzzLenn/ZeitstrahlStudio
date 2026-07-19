namespace ZeitstrahlStudio.App;

/// <summary>Position eines fachlichen Ereignis-Drops relativ zur Zielzeile.</summary>
public enum EventDropPlacement
{
    Before,
    After,
}

/// <summary>Fachlicher Sortierauftrag aus der Ereignisliste.</summary>
public sealed record EventReorderRequest(
    Guid DraggedEventId,
    Guid TargetEventId,
    EventDropPlacement Placement);

/// <summary>Zielgenauer Auftrag zum Import lokaler Dateien als Ereignisanhänge.</summary>
public sealed record AttachmentDropRequest(
    Guid EventId,
    IReadOnlyList<string> Paths);
