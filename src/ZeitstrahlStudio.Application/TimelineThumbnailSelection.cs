using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Application;

/// <summary>Bestimmt projektweit einheitlich den primären visuell darstellbaren Anhang.</summary>
public static class TimelineThumbnailSelection
{
    /// <summary>Bevorzugt das erste PDF und danach das erste unterstützte Bild.</summary>
    public static Attachment? SelectPrimary(TimelineEvent timelineEvent)
    {
        ArgumentNullException.ThrowIfNull(timelineEvent);
        return timelineEvent.Attachments.FirstOrDefault(attachment =>
                   attachment.MediaType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
               ?? timelineEvent.Attachments.FirstOrDefault(attachment =>
                   IsSupportedImage(attachment.MediaType));
    }

    private static bool IsSupportedImage(string mediaType) =>
        mediaType.Equals("image/png", StringComparison.OrdinalIgnoreCase) ||
        mediaType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
        mediaType.Equals("image/tiff", StringComparison.OrdinalIgnoreCase) ||
        mediaType.Equals("image/bmp", StringComparison.OrdinalIgnoreCase);
}
