using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.UnitTests;

public sealed class BackupRetentionPolicyTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SelectAutomaticBackupsToKeep_AppliesCurrentDailyAndWeeklyTiers()
    {
        var policy = new BackupRetentionPolicy();
        var settings = new ProjectSettings
        {
            CurrentDayBackupCount = 2,
            DailyBackupCount = 3,
            WeeklyBackupCount = 2,
        };
        var todayNewest = Create(Now.AddMinutes(-5), automatic: true);
        var todaySecond = Create(Now.AddHours(-1), automatic: true);
        var todayDiscarded = Create(Now.AddHours(-2), automatic: true);
        var yesterdayNewest = Create(Now.AddDays(-1), automatic: true);
        var yesterdayDiscarded = Create(Now.AddDays(-1).AddHours(-2), automatic: true);
        var twoDaysAgo = Create(Now.AddDays(-2), automatic: true);
        var threeDaysAgo = Create(Now.AddDays(-3), automatic: true);
        var olderWeekNewest = Create(new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero), true);
        var olderWeekDiscarded = Create(new DateTimeOffset(2026, 1, 14, 9, 0, 0, TimeSpan.Zero), true);
        var secondOlderWeek = Create(new DateTimeOffset(2026, 1, 8, 9, 0, 0, TimeSpan.Zero), true);
        var thirdOlderWeek = Create(new DateTimeOffset(2025, 12, 31, 9, 0, 0, TimeSpan.Zero), true);
        var manual = Create(Now.AddDays(-50), automatic: false);
        var backups = new[]
        {
            todayNewest,
            todaySecond,
            todayDiscarded,
            yesterdayNewest,
            yesterdayDiscarded,
            twoDaysAgo,
            threeDaysAgo,
            olderWeekNewest,
            olderWeekDiscarded,
            secondOlderWeek,
            thirdOlderWeek,
            manual,
        };

        var keep = policy.SelectAutomaticBackupsToKeep(
            backups,
            settings,
            Now,
            TimeZoneInfo.Utc);

        Assert.Contains(todayNewest.Id, keep);
        Assert.Contains(todaySecond.Id, keep);
        Assert.Contains(yesterdayNewest.Id, keep);
        Assert.Contains(twoDaysAgo.Id, keep);
        Assert.Contains(threeDaysAgo.Id, keep);
        Assert.Contains(olderWeekNewest.Id, keep);
        Assert.Contains(secondOlderWeek.Id, keep);
        Assert.DoesNotContain(todayDiscarded.Id, keep);
        Assert.DoesNotContain(yesterdayDiscarded.Id, keep);
        Assert.DoesNotContain(olderWeekDiscarded.Id, keep);
        Assert.DoesNotContain(thirdOlderWeek.Id, keep);
        Assert.DoesNotContain(manual.Id, keep);
    }

    [Fact]
    public void AutomaticIntervalAndDueState_RespectCurrentDayCount()
    {
        var policy = new BackupRetentionPolicy();
        Assert.Equal(
            TimeSpan.FromMinutes(30),
            policy.GetAutomaticInterval(new ProjectSettings { CurrentDayBackupCount = 48 }));
        Assert.Equal(
            TimeSpan.FromHours(4),
            policy.GetAutomaticInterval(new ProjectSettings { CurrentDayBackupCount = 6 }));
        Assert.Equal(
            TimeSpan.FromHours(24),
            policy.GetAutomaticInterval(new ProjectSettings { CurrentDayBackupCount = 1 }));

        var recent = Create(Now.AddHours(-3), automatic: true);
        Assert.False(policy.IsAutomaticBackupDue(
            [recent],
            new ProjectSettings { CurrentDayBackupCount = 6 },
            Now));
        Assert.True(policy.IsAutomaticBackupDue(
            [recent with { CreatedAtUtc = Now.AddHours(-4) }],
            new ProjectSettings { CurrentDayBackupCount = 6 },
            Now));
        Assert.True(policy.IsAutomaticBackupDue(
            [],
            new ProjectSettings(),
            Now));
    }

    private static BackupRecord Create(DateTimeOffset timestamp, bool automatic) => new(
        Guid.NewGuid(),
        timestamp,
        "project/backup.zeitprojekt",
        123,
        new string('a', 64),
        automatic);
}
