using System.Globalization;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Application;

/// <summary>Bestimmt ohne Dateisystemzugriff Fälligkeit und Rotation automatischer Sicherungen.</summary>
public sealed class BackupRetentionPolicy
{
    private static readonly TimeSpan MinimumAutomaticInterval = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan MaximumAutomaticInterval = TimeSpan.FromHours(24);

    /// <summary>Verteilt die gewünschte Zahl aktueller Sicherungen gleichmäßig über einen Tag.</summary>
    public TimeSpan GetAutomaticInterval(ProjectSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        var calculated = TimeSpan.FromHours(24d / settings.CurrentDayBackupCount);
        return calculated < MinimumAutomaticInterval
            ? MinimumAutomaticInterval
            : calculated > MaximumAutomaticInterval
                ? MaximumAutomaticInterval
                : calculated;
    }

    /// <summary>Prüft anhand der jüngsten automatischen Sicherung, ob ein neuer Snapshot fällig ist.</summary>
    public bool IsAutomaticBackupDue(
        IReadOnlyCollection<BackupRecord> backups,
        ProjectSettings settings,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(backups);
        ValidateUtc(nowUtc, nameof(nowUtc));
        var latest = backups
            .Where(backup => backup.IsAutomatic)
            .OrderByDescending(backup => backup.CreatedAtUtc)
            .FirstOrDefault();
        return latest is null ||
            (latest.CreatedAtUtc <= nowUtc &&
             nowUtc - latest.CreatedAtUtc >= GetAutomaticInterval(settings));
    }

    /// <summary>
    /// Wählt aktuelle, tägliche und danach wöchentliche automatische Sicherungen aus.
    /// Manuelle Sicherungen werden vom Aufrufer grundsätzlich nicht rotiert.
    /// </summary>
    public IReadOnlySet<Guid> SelectAutomaticBackupsToKeep(
        IReadOnlyCollection<BackupRecord> backups,
        ProjectSettings settings,
        DateTimeOffset nowUtc,
        TimeZoneInfo localTimeZone)
    {
        ArgumentNullException.ThrowIfNull(backups);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(localTimeZone);
        settings.Validate();
        ValidateUtc(nowUtc, nameof(nowUtc));

        var automatic = backups
            .Where(backup => backup.IsAutomatic)
            .Select(backup =>
            {
                ValidateBackup(backup);
                return new LocalBackup(
                    backup,
                    DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(backup.CreatedAtUtc, localTimeZone).Date));
            })
            .OrderByDescending(item => item.Record.CreatedAtUtc)
            .ThenByDescending(item => item.Record.Id)
            .ToArray();
        var keep = new HashSet<Guid>();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(nowUtc, localTimeZone).Date);

        foreach (var item in automatic
                     .Where(item => item.LocalDate >= today)
                     .Take(settings.CurrentDayBackupCount))
        {
            keep.Add(item.Record.Id);
        }

        var dailyBoundary = today.AddDays(-settings.DailyBackupCount);
        foreach (var dailyGroup in automatic
                     .Where(item => item.LocalDate < today && item.LocalDate >= dailyBoundary)
                     .GroupBy(item => item.LocalDate))
        {
            keep.Add(dailyGroup.First().Record.Id);
        }

        var weeklyCandidates = automatic
            .Where(item => item.LocalDate < dailyBoundary)
            .GroupBy(item => new IsoWeek(
                ISOWeek.GetYear(item.LocalDate.ToDateTime(TimeOnly.MinValue)),
                ISOWeek.GetWeekOfYear(item.LocalDate.ToDateTime(TimeOnly.MinValue))))
            .Select(group => group.First())
            .OrderByDescending(item => item.LocalDate)
            .ThenByDescending(item => item.Record.CreatedAtUtc)
            .Take(settings.WeeklyBackupCount);
        foreach (var item in weeklyCandidates)
        {
            keep.Add(item.Record.Id);
        }

        return keep;
    }

    private static void ValidateBackup(BackupRecord backup)
    {
        if (backup.Id == Guid.Empty || backup.CreatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidDataException("Die Sicherungsmetadaten enthalten eine ungültige ID oder Zeitangabe.");
        }
    }

    private static void ValidateUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Technische Sicherungszeitpunkte müssen in UTC angegeben werden.", parameterName);
        }
    }

    private sealed record LocalBackup(BackupRecord Record, DateOnly LocalDate);
    private sealed record IsoWeek(int Year, int Week);
}
