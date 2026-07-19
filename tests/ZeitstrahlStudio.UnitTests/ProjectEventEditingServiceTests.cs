using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.UnitTests;

public sealed class ProjectEventEditingServiceTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 7, 19, 10, 0, 0, TimeSpan.Zero);

    private readonly ProjectEventEditingService service = new();

    [Fact]
    public void Create_AddsCompleteEventToProject()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Chronik", CreatedAt);
        var deadline = new Deadline(
            Guid.NewGuid(),
            new DateOnly(2027, 1, 5),
            new TimeOnly(12, 30),
            "Einreichen",
            DeadlineStatus.Open,
            "Unterlagen prüfen");
        var request = CreateRequest(deadline);

        var created = service.Create(project, request, CreatedAt.AddMinutes(1));

        Assert.Same(created, Assert.Single(project.Events));
        Assert.Equal("Wichtig", created.Title);
        Assert.Equal(DatePrecision.DateRange, created.Date.Precision);
        Assert.Equal(deadline, created.Deadline);
        Assert.Equal(["Geschichte", "Quelle"], created.Tags.Order());
        Assert.Equal("https://example.test/quellen", Assert.Single(created.WebLinks).Address.AbsoluteUri);
    }

    [Fact]
    public void Update_PreservesIdsAttachmentsAndManualOrder()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Chronik", CreatedAt);
        var existing = TimelineEvent.Create(
            Guid.NewGuid(),
            "Alt",
            EventDate.Year(1990),
            CreatedAt);
        existing.SetManualSortPosition(12.5m, CreatedAt.AddMinutes(1));
        var attachment = new Attachment(
            Guid.NewGuid(),
            "quelle.pdf",
            "application/pdf",
            10,
            new string('A', 64),
            null,
            CreatedAt,
            "attachments/quelle.pdf",
            AttachmentState.Ready);
        existing.AddAttachment(attachment, CreatedAt.AddMinutes(2));
        var existingLink = new WebLink(Guid.NewGuid(), new Uri("https://example.test/quellen"));
        existing.AddWebLink(existingLink, CreatedAt.AddMinutes(3));
        project.AddEvent(existing, CreatedAt.AddMinutes(4));

        var request = CreateRequest(deadline: null) with
        {
            WebLinks = [new WebLinkInput(null, "https://example.test/quellen", "Quellen")],
        };
        var updated = service.Update(project, existing.Id, request, CreatedAt.AddMinutes(5));

        Assert.Equal(existing.Id, updated.Id);
        Assert.Equal(existing.CreatedAtUtc, updated.CreatedAtUtc);
        Assert.Equal(12.5m, updated.ManualSortPosition);
        Assert.Same(attachment, Assert.Single(updated.Attachments));
        Assert.Equal(existingLink.Id, Assert.Single(updated.WebLinks).Id);
        Assert.Same(updated, Assert.Single(project.Events));
    }

    [Fact]
    public void Update_InvalidLinkDoesNotReplaceExistingEvent()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Chronik", CreatedAt);
        var existing = TimelineEvent.Create(
            Guid.NewGuid(),
            "Alt",
            EventDate.Year(1990),
            CreatedAt);
        project.AddEvent(existing, CreatedAt.AddMinutes(1));
        var invalid = CreateRequest(deadline: null) with
        {
            WebLinks = [new WebLinkInput(null, "kein-link", null)],
        };

        Assert.Throws<DomainValidationException>(() =>
            service.Update(project, existing.Id, invalid, CreatedAt.AddMinutes(2)));
        Assert.Same(existing, Assert.Single(project.Events));
    }

    [Fact]
    public void Delete_RemovesEvent()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Chronik", CreatedAt);
        var existing = service.Create(project, CreateRequest(deadline: null), CreatedAt.AddMinutes(1));

        var deleted = service.Delete(project, existing.Id, CreatedAt.AddMinutes(2));

        Assert.Same(existing, deleted);
        Assert.Empty(project.Events);
    }

    private static EventEditRequest CreateRequest(Deadline? deadline) => new(
        EventDate.Range(new DateOnly(2020, 1, 2), new DateOnly(2020, 3, 4)),
        "Wichtig",
        "Kurzinfo",
        "Beschreibung",
        deadline,
        EventPriority.High,
        "#112233",
        "Archiv",
        "Notiz",
        EventStatus.Active,
        ["Geschichte", "Quelle"],
        [new WebLinkInput(null, "https://example.test/quellen", "Quellen")]);
}
