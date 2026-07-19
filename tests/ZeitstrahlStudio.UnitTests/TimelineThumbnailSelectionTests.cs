using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.UnitTests;

public sealed class TimelineThumbnailSelectionTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SelectPrimary_PrefersFirstPdfOverEarlierImage()
    {
        var timelineEvent = TimelineEvent.Create(
            Guid.NewGuid(),
            "Auswahl",
            EventDate.Year(2026),
            Timestamp);
        var image = CreateAttachment("bild.png", "image/png");
        var pdf = CreateAttachment("quelle.pdf", "application/pdf", linkedPdfPage: 3);
        timelineEvent.AddAttachment(image, Timestamp);
        timelineEvent.AddAttachment(pdf, Timestamp);

        var selected = TimelineThumbnailSelection.SelectPrimary(timelineEvent);

        Assert.Same(pdf, selected);
    }

    [Fact]
    public void SelectPrimary_IgnoresUnsupportedAttachmentAndUsesSupportedImage()
    {
        var timelineEvent = TimelineEvent.Create(
            Guid.NewGuid(),
            "Auswahl",
            EventDate.Year(2026),
            Timestamp);
        timelineEvent.AddAttachment(CreateAttachment("notiz.txt", "text/plain"), Timestamp);
        var image = CreateAttachment("scan.jpg", "image/jpeg");
        timelineEvent.AddAttachment(image, Timestamp);

        Assert.Same(image, TimelineThumbnailSelection.SelectPrimary(timelineEvent));
    }

    private static Attachment CreateAttachment(
        string fileName,
        string mediaType,
        int? linkedPdfPage = null) => new(
        Guid.NewGuid(),
        fileName,
        mediaType,
        1,
        new string('a', 64),
        null,
        Timestamp,
        $"attachments/{Guid.NewGuid():N}/{fileName}",
        linkedPdfPage: linkedPdfPage);
}
