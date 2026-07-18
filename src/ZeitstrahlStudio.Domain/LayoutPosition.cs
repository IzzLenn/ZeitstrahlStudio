namespace ZeitstrahlStudio.Domain;

/// <summary>Optionale manuelle Darstellungsposition, die niemals das Ereignisdatum ändert.</summary>
public sealed record LayoutPosition
{
    public LayoutPosition(Guid eventId, TimelineOrientation orientation, double horizontalOffset, double verticalOffset)
    {
        if (eventId == Guid.Empty)
        {
            throw new DomainValidationException("Eine Layoutposition benötigt eine Ereignis-ID.", nameof(eventId));
        }

        if (!double.IsFinite(horizontalOffset) || !double.IsFinite(verticalOffset))
        {
            throw new DomainValidationException("Layoutversätze müssen endliche Zahlen sein.");
        }

        EventId = eventId;
        Orientation = orientation;
        HorizontalOffset = horizontalOffset;
        VerticalOffset = verticalOffset;
    }

    public Guid EventId { get; }
    public TimelineOrientation Orientation { get; }
    public double HorizontalOffset { get; }
    public double VerticalOffset { get; }
}
