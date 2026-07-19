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

    [Fact]
    public void UndoAndRedo_Create_RestoresBothStates()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Chronik", CreatedAt);
        var created = service.Create(project, CreateRequest(deadline: null), CreatedAt.AddMinutes(1));

        var undo = service.Undo(project, CreatedAt.AddMinutes(2));
        Assert.Empty(project.Events);
        Assert.True(service.CanRedo(project.Id));
        Assert.Null(undo.SelectedEventId);

        var redo = service.Redo(project, CreatedAt.AddMinutes(3));
        Assert.Equal(created.Id, Assert.Single(project.Events).Id);
        Assert.Equal(created.Id, redo.SelectedEventId);
    }

    [Fact]
    public void UndoAndRedo_Update_RestoresSnapshots()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Chronik", CreatedAt);
        var created = service.Create(project, CreateRequest(deadline: null), CreatedAt.AddMinutes(1));
        var changed = CreateRequest(deadline: null) with { Title = "Geändert" };
        service.Update(project, created.Id, changed, CreatedAt.AddMinutes(2));

        service.Undo(project, CreatedAt.AddMinutes(3));
        Assert.Equal("Wichtig", Assert.Single(project.Events).Title);

        service.Redo(project, CreatedAt.AddMinutes(4));
        Assert.Equal("Geändert", Assert.Single(project.Events).Title);
    }

    [Fact]
    public void Undo_Delete_RestoresEvent()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Chronik", CreatedAt);
        var created = service.Create(project, CreateRequest(deadline: null), CreatedAt.AddMinutes(1));
        service.Delete(project, created.Id, CreatedAt.AddMinutes(2));

        service.Undo(project, CreatedAt.AddMinutes(3));

        Assert.Equal(created.Id, Assert.Single(project.Events).Id);
    }

    [Fact]
    public void MoveWithinSameDate_ChangesOnlyEqualDateGroupAndSupportsUndo()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Chronik", CreatedAt);
        var sharedDate = EventDate.Exact(new DateOnly(2020, 1, 2));
        var first = TimelineEvent.Create(Guid.NewGuid(), "A", sharedDate, CreatedAt);
        var second = TimelineEvent.Create(Guid.NewGuid(), "B", sharedDate, CreatedAt.AddSeconds(1));
        var later = TimelineEvent.Create(
            Guid.NewGuid(),
            "C",
            EventDate.Exact(new DateOnly(2020, 1, 3)),
            CreatedAt.AddSeconds(2));
        project.AddEvent(first, CreatedAt.AddMinutes(1));
        project.AddEvent(second, CreatedAt.AddMinutes(1));
        project.AddEvent(later, CreatedAt.AddMinutes(1));

        Assert.True(service.MoveWithinSameDate(
            project,
            second.Id,
            moveEarlier: true,
            CreatedAt.AddMinutes(2)));
        Assert.Equal([second.Id, first.Id, later.Id], project.GetChronologicalEvents().Select(item => item.Id));

        service.Undo(project, CreatedAt.AddMinutes(3));
        Assert.Equal([first.Id, second.Id, later.Id], project.GetChronologicalEvents().Select(item => item.Id));
    }

    [Fact]
    public void AddAndRemoveAttachment_AreUndoable()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Chronik", CreatedAt);
        var timelineEvent = service.Create(
            project,
            CreateRequest(deadline: null),
            CreatedAt.AddMinutes(1));
        var attachment = new Attachment(
            Guid.NewGuid(),
            "beleg.pdf",
            "application/pdf",
            5,
            new string('b', 64),
            null,
            CreatedAt,
            $"attachments/{Guid.NewGuid():N}/beleg.pdf");

        service.AddAttachments(
            project,
            timelineEvent.Id,
            [attachment],
            CreatedAt.AddMinutes(2));
        Assert.Equal(attachment.Id, Assert.Single(project.Events.Single().Attachments).Id);

        service.RemoveAttachment(
            project,
            timelineEvent.Id,
            attachment.Id,
            CreatedAt.AddMinutes(3));
        Assert.Empty(project.Events.Single().Attachments);

        service.Undo(project, CreatedAt.AddMinutes(4));
        Assert.Equal(attachment.Id, Assert.Single(project.Events.Single().Attachments).Id);
        service.Undo(project, CreatedAt.AddMinutes(5));
        Assert.Empty(project.Events.Single().Attachments);
    }

    [Fact]
    public void MoveLayoutPosition_IsOrientationSpecificAndUndoable()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Chronik", CreatedAt);
        var timelineEvent = service.Create(
            project,
            CreateRequest(deadline: null),
            CreatedAt.AddMinutes(1));

        var moved = service.MoveLayoutPosition(
            project,
            timelineEvent.Id,
            TimelineOrientation.Horizontal,
            horizontalDelta: 35,
            verticalDelta: -18,
            CreatedAt.AddMinutes(2));

        Assert.NotNull(moved);
        Assert.Equal(35, moved.HorizontalOffset);
        Assert.Equal(-18, moved.VerticalOffset);
        Assert.Equal(timelineEvent.Date, Assert.Single(project.Events).Date);

        var undo = service.Undo(project, CreatedAt.AddMinutes(3));
        Assert.Empty(project.LayoutPositions);
        Assert.Equal(timelineEvent.Id, undo.SelectedEventId);

        service.Redo(project, CreatedAt.AddMinutes(4));
        Assert.Equal(moved, Assert.Single(project.LayoutPositions));
    }

    [Fact]
    public void ResetLayoutPositions_IsOneUndoableOperation()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Chronik", CreatedAt);
        var timelineEvent = service.Create(
            project,
            CreateRequest(deadline: null),
            CreatedAt.AddMinutes(1));
        service.MoveLayoutPosition(
            project,
            timelineEvent.Id,
            TimelineOrientation.Horizontal,
            20,
            30,
            CreatedAt.AddMinutes(2));
        service.MoveLayoutPosition(
            project,
            timelineEvent.Id,
            TimelineOrientation.Vertical,
            -40,
            50,
            CreatedAt.AddMinutes(3));

        Assert.True(service.ResetLayoutPositions(
            project,
            timelineEvent.Id,
            CreatedAt.AddMinutes(4)));
        Assert.Empty(project.LayoutPositions);

        service.Undo(project, CreatedAt.AddMinutes(5));
        Assert.Equal(2, project.LayoutPositions.Count);
        Assert.Contains(project.LayoutPositions, position =>
            position.Orientation == TimelineOrientation.Horizontal);
        Assert.Contains(project.LayoutPositions, position =>
            position.Orientation == TimelineOrientation.Vertical);
    }

    [Fact]
    public void UndoDelete_RestoresManualLayoutPositions()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Chronik", CreatedAt);
        var timelineEvent = service.Create(
            project,
            CreateRequest(deadline: null),
            CreatedAt.AddMinutes(1));
        service.MoveLayoutPosition(
            project,
            timelineEvent.Id,
            TimelineOrientation.Horizontal,
            12,
            -7,
            CreatedAt.AddMinutes(2));
        service.Delete(project, timelineEvent.Id, CreatedAt.AddMinutes(3));
        Assert.Empty(project.LayoutPositions);

        service.Undo(project, CreatedAt.AddMinutes(4));

        var restored = Assert.Single(project.LayoutPositions);
        Assert.Equal(timelineEvent.Id, restored.EventId);
        Assert.Equal(12, restored.HorizontalOffset);
        Assert.Equal(-7, restored.VerticalOffset);
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
