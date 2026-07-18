using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.UnitTests;

public sealed class DeadlineTests
{
    private static readonly DateTime ReferenceTime = new(2026, 7, 19, 12, 0, 0, DateTimeKind.Local);

    [Theory]
    [InlineData(DeadlineStatus.Completed, 20, DeadlineUrgency.Completed)]
    [InlineData(DeadlineStatus.Cancelled, 20, DeadlineUrgency.None)]
    [InlineData(DeadlineStatus.Open, 18, DeadlineUrgency.Overdue)]
    [InlineData(DeadlineStatus.Open, 20, DeadlineUrgency.Upcoming)]
    [InlineData(DeadlineStatus.Open, 30, DeadlineUrgency.Open)]
    public void GetUrgency_ClassifiesDeadline(
        DeadlineStatus status,
        int dueDay,
        DeadlineUrgency expected)
    {
        var deadline = new Deadline(Guid.NewGuid(), new DateOnly(2026, 7, dueDay), status: status);

        var urgency = deadline.GetUrgency(ReferenceTime, TimeSpan.FromDays(3));

        Assert.Equal(expected, urgency);
    }

    [Fact]
    public void GetUrgency_UsesExactTimeWhenPresent()
    {
        var deadline = new Deadline(
            Guid.NewGuid(),
            new DateOnly(2026, 7, 19),
            new TimeOnly(11, 59));

        Assert.Equal(DeadlineUrgency.Overdue, deadline.GetUrgency(ReferenceTime, TimeSpan.FromDays(3)));
    }
}
