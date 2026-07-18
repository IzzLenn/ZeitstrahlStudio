using System.Globalization;
using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Infrastructure;

public sealed partial class SqliteProjectRepository
{
    private static async Task<TimelineProject> LoadProjectAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var header = await LoadProjectHeaderAsync(connection, cancellationToken).ConfigureAwait(false);
        var attachments = await LoadAttachmentsAsync(connection, header.Id, cancellationToken).ConfigureAwait(false);
        var tags = await LoadTagsAsync(connection, header.Id, cancellationToken).ConfigureAwait(false);
        var webLinks = await LoadWebLinksAsync(connection, header.Id, cancellationToken).ConfigureAwait(false);
        var events = await LoadEventsAsync(
            connection,
            header.Id,
            attachments,
            tags,
            webLinks,
            cancellationToken).ConfigureAwait(false);
        var layoutPositions = await LoadLayoutPositionsAsync(connection, header.Id, cancellationToken)
            .ConfigureAwait(false);

        return TimelineProject.Restore(
            header.Id,
            header.Name,
            header.Subtitle,
            header.InfoText,
            header.Description,
            header.OverallStart,
            header.OverallEnd,
            header.CreatedAtUtc,
            header.ModifiedAtUtc,
            header.Settings,
            events,
            layoutPositions);
    }

    private static async Task<ProjectHeader> LoadProjectHeaderAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM Projects;";
        var projectCount = Convert.ToInt32(
            await countCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
            CultureInfo.InvariantCulture);
        if (projectCount == 0)
        {
            throw new InvalidDataException("Die Projektdatenbank enthält kein Projekt.");
        }

        if (projectCount > 1)
        {
            throw new InvalidDataException("Die Projektdatenbank enthält unerwartet mehrere Projekte.");
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                p.Id, p.Name, p.Subtitle, p.InfoText, p.Description,
                p.OverallStart, p.OverallEnd, p.CreatedAtUtc, p.ModifiedAtUtc,
                s.PreferredOrientation, s.Theme, s.DefaultEventColorHex,
                s.TimelineCardFontSize, s.TimelineAxisFontSize, s.ExportFontSize,
                s.CompressLargeGaps, s.AutoSaveIntervalSeconds, s.CurrentDayBackupCount,
                s.DailyBackupCount, s.WeeklyBackupCount
            FROM Projects p
            JOIN ProjectSettings s ON s.ProjectId = p.Id;
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidDataException("Die Projekteinstellungen fehlen oder sind beschädigt.");
        }

        var settings = new ProjectSettings
        {
            PreferredOrientation = ReadEnum<TimelineOrientation>(reader.GetInt32(9), "Ausrichtung"),
            Theme = ReadEnum<ApplicationTheme>(reader.GetInt32(10), "Farbschema"),
            DefaultEventColorHex = reader.GetString(11),
            TimelineCardFontSize = reader.GetDouble(12),
            TimelineAxisFontSize = reader.GetDouble(13),
            ExportFontSize = reader.GetDouble(14),
            CompressLargeGaps = reader.GetInt32(15) != 0,
            AutoSaveIntervalSeconds = reader.GetInt32(16),
            CurrentDayBackupCount = reader.GetInt32(17),
            DailyBackupCount = reader.GetInt32(18),
            WeeklyBackupCount = reader.GetInt32(19),
        };
        settings.Validate();

        return new ProjectHeader(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            GetNullableString(reader, 2),
            GetNullableString(reader, 3),
            GetNullableString(reader, 4),
            ParseNullableDate(reader, 5),
            ParseNullableDate(reader, 6),
            ParseTimestamp(reader.GetString(7)),
            ParseTimestamp(reader.GetString(8)),
            settings);
    }

    private static async Task<Dictionary<Guid, List<Attachment>>> LoadAttachmentsAsync(
        SqliteConnection connection,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, List<Attachment>>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                a.EventId, a.Id, a.OriginalFileName, a.MediaType, a.FileSize, a.Sha256,
                a.OriginalSourcePath, a.ImportedAtUtc, a.ProjectRelativePath, a.State,
                a.LinkedPdfPage
            FROM Attachments a
            JOIN Events e ON e.Id = a.EventId
            WHERE e.ProjectId = $projectId
            ORDER BY a.ImportedAtUtc, a.Id;
            """;
        command.Parameters.AddWithValue("$projectId", projectId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var eventId = Guid.Parse(reader.GetString(0));
            var attachment = new Attachment(
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt64(4),
                reader.GetString(5),
                GetNullableString(reader, 6),
                ParseTimestamp(reader.GetString(7)),
                reader.GetString(8),
                ReadEnum<AttachmentState>(reader.GetInt32(9), "Anhangszustand"),
                GetNullableInt32(reader, 10));
            AddToLookup(result, eventId, attachment);
        }

        return result;
    }

    private static async Task<Dictionary<Guid, List<string>>> LoadTagsAsync(
        SqliteConnection connection,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, List<string>>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT et.EventId, t.Name
            FROM EventTags et
            JOIN Tags t ON t.Id = et.TagId
            JOIN Events e ON e.Id = et.EventId
            WHERE e.ProjectId = $projectId
            ORDER BY t.Name COLLATE NOCASE;
            """;
        command.Parameters.AddWithValue("$projectId", projectId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            AddToLookup(result, Guid.Parse(reader.GetString(0)), reader.GetString(1));
        }

        return result;
    }

    private static async Task<Dictionary<Guid, List<WebLink>>> LoadWebLinksAsync(
        SqliteConnection connection,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, List<WebLink>>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT w.EventId, w.Id, w.Address, w.Label
            FROM WebLinks w
            JOIN Events e ON e.Id = w.EventId
            WHERE e.ProjectId = $projectId
            ORDER BY w.Id;
            """;
        command.Parameters.AddWithValue("$projectId", projectId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var eventId = Guid.Parse(reader.GetString(0));
            var webLink = new WebLink(
                Guid.Parse(reader.GetString(1)),
                new Uri(reader.GetString(2), UriKind.Absolute),
                GetNullableString(reader, 3));
            AddToLookup(result, eventId, webLink);
        }

        return result;
    }

    private static async Task<IReadOnlyList<TimelineEvent>> LoadEventsAsync(
        SqliteConnection connection,
        Guid projectId,
        IReadOnlyDictionary<Guid, List<Attachment>> attachments,
        IReadOnlyDictionary<Guid, List<string>> tags,
        IReadOnlyDictionary<Guid, List<WebLink>> webLinks,
        CancellationToken cancellationToken)
    {
        var result = new List<TimelineEvent>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                e.Id, e.Title, e.InfoText, e.Description, e.Priority, e.ColorHex,
                e.Source, e.Notes, e.Status, e.ManualSortPosition,
                e.CreatedAtUtc, e.ModifiedAtUtc,
                ed.Precision, ed.StartYear, ed.StartMonth, ed.StartDay, ed.StartTime,
                ed.EndYear, ed.EndMonth, ed.EndDay,
                d.Id, d.DueDate, d.DueTime, d.Label, d.Status, d.ReminderNote
            FROM Events e
            JOIN EventDates ed ON ed.EventId = e.Id
            LEFT JOIN Deadlines d ON d.EventId = e.Id
            WHERE e.ProjectId = $projectId
            ORDER BY ed.StartYear, COALESCE(ed.StartMonth, 1), COALESCE(ed.StartDay, 1),
                     COALESCE(ed.StartTime, ''), e.CreatedAtUtc, e.Id;
            """;
        command.Parameters.AddWithValue("$projectId", projectId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var eventId = Guid.Parse(reader.GetString(0));
            var eventDate = RestoreEventDate(reader);
            var deadline = RestoreDeadline(reader);
            result.Add(TimelineEvent.Restore(
                eventId,
                eventDate,
                reader.GetString(1),
                GetNullableString(reader, 2),
                GetNullableString(reader, 3),
                deadline,
                ReadEnum<EventPriority>(reader.GetInt32(4), "Priorität"),
                reader.GetString(5),
                GetNullableString(reader, 6),
                GetNullableString(reader, 7),
                ReadEnum<EventStatus>(reader.GetInt32(8), "Ereignisstatus"),
                ParseNullableDecimal(reader, 9),
                ParseTimestamp(reader.GetString(10)),
                ParseTimestamp(reader.GetString(11)),
                tags.GetValueOrDefault(eventId) ?? [],
                attachments.GetValueOrDefault(eventId) ?? [],
                webLinks.GetValueOrDefault(eventId) ?? []));
        }

        return result;
    }

    private static async Task<IReadOnlyList<LayoutPosition>> LoadLayoutPositionsAsync(
        SqliteConnection connection,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = new List<LayoutPosition>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT l.EventId, l.Orientation, l.HorizontalOffset, l.VerticalOffset
            FROM LayoutPositions l
            JOIN Events e ON e.Id = l.EventId
            WHERE e.ProjectId = $projectId
            ORDER BY l.EventId, l.Orientation;
            """;
        command.Parameters.AddWithValue("$projectId", projectId.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result.Add(new LayoutPosition(
                Guid.Parse(reader.GetString(0)),
                ReadEnum<TimelineOrientation>(reader.GetInt32(1), "Layoutausrichtung"),
                reader.GetDouble(2),
                reader.GetDouble(3)));
        }

        return result;
    }

    private static EventDate RestoreEventDate(SqliteDataReader reader)
    {
        var precision = ReadEnum<DatePrecision>(reader.GetInt32(12), "Datumsgenauigkeit");
        var startYear = reader.GetInt32(13);
        var startMonth = GetNullableInt32(reader, 14);
        var startDay = GetNullableInt32(reader, 15);
        var startTime = ParseNullableTime(reader, 16);
        var endYear = GetNullableInt32(reader, 17);
        var endMonth = GetNullableInt32(reader, 18);
        var endDay = GetNullableInt32(reader, 19);

        return precision switch
        {
            DatePrecision.Year => EventDate.Year(startYear),
            DatePrecision.MonthAndYear => EventDate.MonthAndYear(startYear, Require(startMonth, "Startmonat")),
            DatePrecision.ExactDate => EventDate.Exact(new DateOnly(
                startYear,
                Require(startMonth, "Startmonat"),
                Require(startDay, "Starttag"))),
            DatePrecision.ExactDateTime => EventDate.ExactWithTime(
                new DateOnly(startYear, Require(startMonth, "Startmonat"), Require(startDay, "Starttag")),
                startTime ?? throw new InvalidDataException("Die Uhrzeit eines Ereignisses fehlt.")),
            DatePrecision.DateRange => EventDate.Range(
                new DateOnly(startYear, Require(startMonth, "Startmonat"), Require(startDay, "Starttag")),
                new DateOnly(
                    Require(endYear, "Endjahr"),
                    Require(endMonth, "Endmonat"),
                    Require(endDay, "Endtag"))),
            _ => throw new InvalidDataException("Die Datumsgenauigkeit wird nicht unterstützt."),
        };
    }

    private static Deadline? RestoreDeadline(SqliteDataReader reader)
    {
        if (reader.IsDBNull(20))
        {
            return null;
        }

        return new Deadline(
            Guid.Parse(reader.GetString(20)),
            ParseDate(reader.GetString(21)),
            ParseNullableTime(reader, 22),
            GetNullableString(reader, 23),
            ReadEnum<DeadlineStatus>(reader.GetInt32(24), "Friststatus"),
            GetNullableString(reader, 25));
    }

    private static void AddToLookup<T>(Dictionary<Guid, List<T>> lookup, Guid key, T value)
    {
        if (!lookup.TryGetValue(key, out var values))
        {
            values = [];
            lookup.Add(key, values);
        }

        values.Add(value);
    }

    private static TEnum ReadEnum<TEnum>(int value, string fieldName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(typeof(TEnum), value))
        {
            throw new InvalidDataException($"Das Feld '{fieldName}' enthält den unbekannten Wert {value}.");
        }

        return (TEnum)Enum.ToObject(typeof(TEnum), value);
    }

    private static int Require(int? value, string fieldName) =>
        value ?? throw new InvalidDataException($"Das erforderliche Datumsfeld '{fieldName}' fehlt.");

    private static string? GetNullableString(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static int? GetNullableInt32(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);

    private static DateOnly? ParseNullableDate(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : ParseDate(reader.GetString(ordinal));

    private static DateOnly ParseDate(string value) =>
        DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static TimeOnly? ParseNullableTime(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal)
            ? null
            : TimeOnly.ParseExact(reader.GetString(ordinal), "HH:mm:ss.fffffff", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseTimestamp(string value) =>
        DateTimeOffset.ParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private static decimal? ParseNullableDecimal(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal)
            ? null
            : decimal.Parse(reader.GetString(ordinal), NumberStyles.Number, CultureInfo.InvariantCulture);

    private sealed record ProjectHeader(
        Guid Id,
        string Name,
        string? Subtitle,
        string? InfoText,
        string? Description,
        DateOnly? OverallStart,
        DateOnly? OverallEnd,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset ModifiedAtUtc,
        ProjectSettings Settings);
}
