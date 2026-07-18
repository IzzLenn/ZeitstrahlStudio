using System.Globalization;
using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Infrastructure;

public sealed partial class SqliteProjectRepository
{
    private static async Task PrepareStagingTablesAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            CREATE TEMP TABLE IF NOT EXISTS SaveEventIds (Id TEXT NOT NULL PRIMARY KEY);
            CREATE TEMP TABLE IF NOT EXISTS SaveAttachmentIds (Id TEXT NOT NULL PRIMARY KEY);
            CREATE TEMP TABLE IF NOT EXISTS SaveWebLinkIds (Id TEXT NOT NULL PRIMARY KEY);
            DELETE FROM SaveEventIds;
            DELETE FROM SaveAttachmentIds;
            DELETE FROM SaveWebLinkIds;
            """);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task StageIdAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string tableName,
        Guid id,
        CancellationToken cancellationToken)
    {
        var allowedTable = tableName switch
        {
            "SaveEventIds" => "SaveEventIds",
            "SaveAttachmentIds" => "SaveAttachmentIds",
            "SaveWebLinkIds" => "SaveWebLinkIds",
            _ => throw new ArgumentOutOfRangeException(nameof(tableName)),
        };

        await using var command = CreateCommand(
            connection,
            transaction,
            $"INSERT INTO {allowedTable} (Id) VALUES ($id);");
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task UpsertProjectAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        TimelineProject project,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            INSERT INTO Projects (
                Id, Name, Subtitle, InfoText, Description, OverallStart, OverallEnd,
                CreatedAtUtc, ModifiedAtUtc)
            VALUES (
                $id, $name, $subtitle, $infoText, $description, $overallStart, $overallEnd,
                $createdAtUtc, $modifiedAtUtc)
            ON CONFLICT(Id) DO UPDATE SET
                Name = excluded.Name,
                Subtitle = excluded.Subtitle,
                InfoText = excluded.InfoText,
                Description = excluded.Description,
                OverallStart = excluded.OverallStart,
                OverallEnd = excluded.OverallEnd,
                CreatedAtUtc = excluded.CreatedAtUtc,
                ModifiedAtUtc = excluded.ModifiedAtUtc;
            """);
        AddParameter(command, "$id", project.Id.ToString("D"));
        AddParameter(command, "$name", project.Name);
        AddParameter(command, "$subtitle", project.Subtitle);
        AddParameter(command, "$infoText", project.InfoText);
        AddParameter(command, "$description", project.Description);
        AddParameter(command, "$overallStart", FormatDate(project.OverallStart));
        AddParameter(command, "$overallEnd", FormatDate(project.OverallEnd));
        AddParameter(command, "$createdAtUtc", FormatTimestamp(project.CreatedAtUtc));
        AddParameter(command, "$modifiedAtUtc", FormatTimestamp(project.ModifiedAtUtc));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task UpsertSettingsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        TimelineProject project,
        CancellationToken cancellationToken)
    {
        var settings = project.Settings;
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            INSERT INTO ProjectSettings (
                ProjectId, PreferredOrientation, Theme, DefaultEventColorHex,
                TimelineCardFontSize, TimelineAxisFontSize, ExportFontSize,
                CompressLargeGaps, AutoSaveIntervalSeconds, CurrentDayBackupCount,
                DailyBackupCount, WeeklyBackupCount)
            VALUES (
                $projectId, $preferredOrientation, $theme, $defaultEventColorHex,
                $timelineCardFontSize, $timelineAxisFontSize, $exportFontSize,
                $compressLargeGaps, $autoSaveIntervalSeconds, $currentDayBackupCount,
                $dailyBackupCount, $weeklyBackupCount)
            ON CONFLICT(ProjectId) DO UPDATE SET
                PreferredOrientation = excluded.PreferredOrientation,
                Theme = excluded.Theme,
                DefaultEventColorHex = excluded.DefaultEventColorHex,
                TimelineCardFontSize = excluded.TimelineCardFontSize,
                TimelineAxisFontSize = excluded.TimelineAxisFontSize,
                ExportFontSize = excluded.ExportFontSize,
                CompressLargeGaps = excluded.CompressLargeGaps,
                AutoSaveIntervalSeconds = excluded.AutoSaveIntervalSeconds,
                CurrentDayBackupCount = excluded.CurrentDayBackupCount,
                DailyBackupCount = excluded.DailyBackupCount,
                WeeklyBackupCount = excluded.WeeklyBackupCount;
            """);
        AddParameter(command, "$projectId", project.Id.ToString("D"));
        AddParameter(command, "$preferredOrientation", (int)settings.PreferredOrientation);
        AddParameter(command, "$theme", (int)settings.Theme);
        AddParameter(command, "$defaultEventColorHex", settings.DefaultEventColorHex);
        AddParameter(command, "$timelineCardFontSize", settings.TimelineCardFontSize);
        AddParameter(command, "$timelineAxisFontSize", settings.TimelineAxisFontSize);
        AddParameter(command, "$exportFontSize", settings.ExportFontSize);
        AddParameter(command, "$compressLargeGaps", settings.CompressLargeGaps ? 1 : 0);
        AddParameter(command, "$autoSaveIntervalSeconds", settings.AutoSaveIntervalSeconds);
        AddParameter(command, "$currentDayBackupCount", settings.CurrentDayBackupCount);
        AddParameter(command, "$dailyBackupCount", settings.DailyBackupCount);
        AddParameter(command, "$weeklyBackupCount", settings.WeeklyBackupCount);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task UpsertEventAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid projectId,
        TimelineEvent timelineEvent,
        CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            INSERT INTO Events (
                Id, ProjectId, Title, InfoText, Description, Priority, ColorHex, Source,
                Notes, Status, ManualSortPosition, CreatedAtUtc, ModifiedAtUtc)
            VALUES (
                $id, $projectId, $title, $infoText, $description, $priority, $colorHex, $source,
                $notes, $status, $manualSortPosition, $createdAtUtc, $modifiedAtUtc)
            ON CONFLICT(Id) DO UPDATE SET
                ProjectId = excluded.ProjectId,
                Title = excluded.Title,
                InfoText = excluded.InfoText,
                Description = excluded.Description,
                Priority = excluded.Priority,
                ColorHex = excluded.ColorHex,
                Source = excluded.Source,
                Notes = excluded.Notes,
                Status = excluded.Status,
                ManualSortPosition = excluded.ManualSortPosition,
                CreatedAtUtc = excluded.CreatedAtUtc,
                ModifiedAtUtc = excluded.ModifiedAtUtc;
            """);
        AddParameter(command, "$id", timelineEvent.Id.ToString("D"));
        AddParameter(command, "$projectId", projectId.ToString("D"));
        AddParameter(command, "$title", timelineEvent.Title);
        AddParameter(command, "$infoText", timelineEvent.InfoText);
        AddParameter(command, "$description", timelineEvent.Description);
        AddParameter(command, "$priority", (int)timelineEvent.Priority);
        AddParameter(command, "$colorHex", timelineEvent.ColorHex);
        AddParameter(command, "$source", timelineEvent.Source);
        AddParameter(command, "$notes", timelineEvent.Notes);
        AddParameter(command, "$status", (int)timelineEvent.Status);
        AddParameter(command, "$manualSortPosition", timelineEvent.ManualSortPosition?.ToString("G29", CultureInfo.InvariantCulture));
        AddParameter(command, "$createdAtUtc", FormatTimestamp(timelineEvent.CreatedAtUtc));
        AddParameter(command, "$modifiedAtUtc", FormatTimestamp(timelineEvent.ModifiedAtUtc));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task UpsertEventDateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        TimelineEvent timelineEvent,
        CancellationToken cancellationToken)
    {
        var date = timelineEvent.Date;
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            INSERT INTO EventDates (
                EventId, Precision, StartYear, StartMonth, StartDay, StartTime,
                EndYear, EndMonth, EndDay)
            VALUES (
                $eventId, $precision, $startYear, $startMonth, $startDay, $startTime,
                $endYear, $endMonth, $endDay)
            ON CONFLICT(EventId) DO UPDATE SET
                Precision = excluded.Precision,
                StartYear = excluded.StartYear,
                StartMonth = excluded.StartMonth,
                StartDay = excluded.StartDay,
                StartTime = excluded.StartTime,
                EndYear = excluded.EndYear,
                EndMonth = excluded.EndMonth,
                EndDay = excluded.EndDay;
            """);
        AddParameter(command, "$eventId", timelineEvent.Id.ToString("D"));
        AddParameter(command, "$precision", (int)date.Precision);
        AddParameter(command, "$startYear", date.StartYear);
        AddParameter(command, "$startMonth", date.StartMonth);
        AddParameter(command, "$startDay", date.StartDay);
        AddParameter(command, "$startTime", FormatTime(date.StartTime));
        AddParameter(command, "$endYear", date.EndYear);
        AddParameter(command, "$endMonth", date.EndMonth);
        AddParameter(command, "$endDay", date.EndDay);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task SynchronizeDeadlineAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        TimelineEvent timelineEvent,
        CancellationToken cancellationToken)
    {
        if (timelineEvent.Deadline is null)
        {
            await using var deleteCommand = CreateCommand(
                connection,
                transaction,
                "DELETE FROM Deadlines WHERE EventId = $eventId;");
            deleteCommand.Parameters.AddWithValue("$eventId", timelineEvent.Id.ToString("D"));
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var deadline = timelineEvent.Deadline;
        await using var command = CreateCommand(
            connection,
            transaction,
            """
            DELETE FROM Deadlines WHERE EventId = $eventId AND Id <> $id;
            INSERT INTO Deadlines (Id, EventId, DueDate, DueTime, Label, Status, ReminderNote)
            VALUES ($id, $eventId, $dueDate, $dueTime, $label, $status, $reminderNote)
            ON CONFLICT(Id) DO UPDATE SET
                EventId = excluded.EventId,
                DueDate = excluded.DueDate,
                DueTime = excluded.DueTime,
                Label = excluded.Label,
                Status = excluded.Status,
                ReminderNote = excluded.ReminderNote;
            """);
        AddParameter(command, "$id", deadline.Id.ToString("D"));
        AddParameter(command, "$eventId", timelineEvent.Id.ToString("D"));
        AddParameter(command, "$dueDate", FormatDate(deadline.DueDate));
        AddParameter(command, "$dueTime", FormatTime(deadline.DueTime));
        AddParameter(command, "$label", deadline.Label);
        AddParameter(command, "$status", (int)deadline.Status);
        AddParameter(command, "$reminderNote", deadline.ReminderNote);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string? FormatDate(DateOnly? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string? FormatTime(TimeOnly? value) =>
        value?.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture);

    private static string FormatTimestamp(DateTimeOffset value) =>
        value.ToString("O", CultureInfo.InvariantCulture);
}
