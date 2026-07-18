using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.UnitTests;

public sealed class TimelineProjectTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 19, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AddEvent_RejectsDuplicateId()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Testprojekt", CreatedAt);
        var eventId = Guid.NewGuid();
        var first = TimelineEvent.Create(eventId, "Erstes Ereignis", EventDate.Year(2020), CreatedAt);
        var duplicate = TimelineEvent.Create(eventId, "Duplikat", EventDate.Year(2021), CreatedAt);
        project.AddEvent(first, CreatedAt.AddMinutes(1));

        Assert.Throws<DomainValidationException>(() =>
            project.AddEvent(duplicate, CreatedAt.AddMinutes(2)));
    }

    [Fact]
    public void ChronologicalOrder_UsesManualPositionOnlyForIdenticalDates()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Testprojekt", CreatedAt);
        var sharedDate = EventDate.Exact(new DateOnly(2024, 2, 1));
        var firstCreated = TimelineEvent.Create(Guid.NewGuid(), "A", sharedDate, CreatedAt);
        var secondCreated = TimelineEvent.Create(Guid.NewGuid(), "B", sharedDate, CreatedAt.AddMinutes(1));
        firstCreated.SetManualSortPosition(20, CreatedAt.AddMinutes(2));
        secondCreated.SetManualSortPosition(10, CreatedAt.AddMinutes(2));
        project.AddEvent(firstCreated, CreatedAt.AddMinutes(3));
        project.AddEvent(secondCreated, CreatedAt.AddMinutes(3));

        var sorted = project.GetChronologicalEvents();

        Assert.Equal(new[] { secondCreated, firstCreated }, sorted);
    }

    [Fact]
    public void UpdateInformation_RejectsInvalidOverallRange()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Testprojekt", CreatedAt);

        Assert.Throws<DomainValidationException>(() => project.UpdateInformation(
            "Testprojekt",
            null,
            null,
            null,
            new DateOnly(2025, 1, 1),
            new DateOnly(2024, 1, 1),
            CreatedAt.AddMinutes(1)));
    }

    [Fact]
    public void Event_AcceptsLongTextsWithoutTruncation()
    {
        var longText = new string('x', 100_000);
        var timelineEvent = TimelineEvent.Create(
            Guid.NewGuid(),
            "Langer Inhalt",
            EventDate.Year(2026),
            CreatedAt);

        timelineEvent.UpdateContent(
            "Langer Inhalt",
            longText,
            longText,
            null,
            longText,
            CreatedAt.AddMinutes(1));

        Assert.Equal(longText.Length, timelineEvent.Description!.Length);
        Assert.Equal(longText.Length, timelineEvent.Notes!.Length);
    }
}
