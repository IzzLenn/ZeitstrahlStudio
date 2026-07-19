using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Infrastructure;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class SqliteProjectRepositoryTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 7, 19, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task InitializeAsync_CreatesCompleteVersionedSchemaAndIsIdempotent()
    {
        await using var database = new TemporaryDatabase();
        var repository = new SqliteProjectRepository();

        await repository.InitializeAsync(database.Path, CancellationToken.None);
        await repository.InitializeAsync(database.Path, CancellationToken.None);

        await using var connection = new SqliteConnection($"Data Source={database.Path}");
        await connection.OpenAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type IN ('table', 'view');";
        await using var reader = await command.ExecuteReaderAsync(CancellationToken.None);
        var names = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadAsync(CancellationToken.None))
        {
            names.Add(reader.GetString(0));
        }

        var requiredTables = new[]
        {
            "Projects", "Events", "EventDates", "Deadlines", "Attachments",
            "AttachmentMetadata", "ExtractedTexts", "WebLinks", "Tags", "EventTags",
            "LayoutPositions", "ProjectSettings", "AuditLog", "ApplicationLogReferences",
            "Backups", "SchemaMigrations", "SearchIndex", "DocumentSearchIndex",
        };
        Assert.All(requiredTables, name => Assert.Contains(name, names));

        await reader.DisposeAsync();
        command.CommandText = "SELECT MAX(Version) FROM SchemaMigrations;";
        Assert.Equal(SqliteSchemaMigrator.CurrentVersion, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task InitializeAsync_UpgradesVersionOneWithDocumentOnlySearchIndex()
    {
        await using var database = new TemporaryDatabase();
        var repository = new SqliteProjectRepository();
        await repository.InitializeAsync(database.Path, CancellationToken.None);
        await using (var connection = new SqliteConnection($"Data Source={database.Path}"))
        {
            await connection.OpenAsync(CancellationToken.None);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                DROP TABLE DocumentSearchIndex;
                DELETE FROM SchemaMigrations WHERE Version = 2;
                """;
            await command.ExecuteNonQueryAsync(CancellationToken.None);
        }

        await repository.InitializeAsync(database.Path, CancellationToken.None);

        await using var verification = new SqliteConnection($"Data Source={database.Path}");
        await verification.OpenAsync(CancellationToken.None);
        await using var verificationCommand = verification.CreateCommand();
        verificationCommand.CommandText = """
            SELECT
                (SELECT COUNT(*) FROM sqlite_master WHERE name = 'DocumentSearchIndex'),
                (SELECT MAX(Version) FROM SchemaMigrations);
            """;
        await using var reader = await verificationCommand.ExecuteReaderAsync(CancellationToken.None);
        Assert.True(await reader.ReadAsync(CancellationToken.None));
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal(SqliteSchemaMigrator.CurrentVersion, reader.GetInt32(1));
    }

    [Fact]
    public async Task SaveAndLoadAsync_PreservesCompleteProjectAndDatePrecision()
    {
        await using var database = new TemporaryDatabase();
        var repository = new SqliteProjectRepository();
        var expected = CreateRichProject();

        await repository.SaveAsync(expected, database.Path, CancellationToken.None);
        var actual = await repository.LoadAsync(database.Path, CancellationToken.None);

        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(ApplicationTheme.Dark, actual.Settings.Theme);
        Assert.Equal(TimelineOrientation.Vertical, actual.Settings.PreferredOrientation);
        Assert.Equal(5, actual.Events.Count);

        var year = Assert.Single(actual.Events, item => item.Title == "Nur Jahr");
        Assert.Equal(DatePrecision.Year, year.Date.Precision);
        Assert.Null(year.Date.StartMonth);
        Assert.Null(year.Date.StartDay);

        var month = Assert.Single(actual.Events, item => item.Title == "Monat und Jahr");
        Assert.Equal(DatePrecision.MonthAndYear, month.Date.Precision);
        Assert.Equal(5, month.Date.StartMonth);
        Assert.Null(month.Date.StartDay);

        var exactTime = Assert.Single(actual.Events, item => item.Title == "Datum mit Uhrzeit");
        Assert.Equal(new TimeOnly(9, 30), exactTime.Date.StartTime);
        Assert.Equal(DeadlineStatus.Open, exactTime.Deadline!.Status);
        Assert.Contains("Planung", exactTime.Tags);
        Assert.Equal("#AABBCC", exactTime.ColorHex);
        Assert.Equal(12.5m, exactTime.ManualSortPosition);
        Assert.Single(exactTime.Attachments);
        Assert.Single(exactTime.WebLinks);

        var range = Assert.Single(actual.Events, item => item.Title == "Zeitraum");
        Assert.Equal(DatePrecision.DateRange, range.Date.Precision);
        Assert.Equal(2025, range.Date.EndYear);
        Assert.Equal(3, range.Date.EndDay);

        var position = Assert.Single(actual.LayoutPositions);
        Assert.Equal(exactTime.Id, position.EventId);
        Assert.Equal(TimelineOrientation.Horizontal, position.Orientation);
        Assert.Equal(17.25, position.HorizontalOffset);
    }

    [Fact]
    public async Task SaveAsync_PreservesAnalysisForRetainedAttachmentAndRefreshesSearchIndex()
    {
        await using var database = new TemporaryDatabase();
        var repository = new SqliteProjectRepository();
        var project = CreateRichProject();
        await repository.SaveAsync(project, database.Path, CancellationToken.None);
        var attachment = project.Events.SelectMany(item => item.Attachments).Single();

        await using (var connection = await OpenWithForeignKeysAsync(database.Path))
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                INSERT INTO ExtractedTexts (
                    AttachmentId, Content, ExtractionMethod, Language, ExtractedAtUtc)
                VALUES ($attachmentId, 'lokaler extrahierter Prüftext', 1, 'deu', $timestamp);
                """;
            command.Parameters.AddWithValue("$attachmentId", attachment.Id.ToString("D"));
            command.Parameters.AddWithValue("$timestamp", BaseTime.ToString("O"));
            await command.ExecuteNonQueryAsync(CancellationToken.None);
        }

        var changedEvent = project.Events.Single(item => item.Title == "Datum mit Uhrzeit");
        changedEvent.UpdateContent(
            "Datum mit Uhrzeit",
            "Aktualisierte Beschreibung",
            changedEvent.Description,
            changedEvent.Source,
            changedEvent.Notes,
            BaseTime.AddHours(2));
        await repository.SaveAsync(project, database.Path, CancellationToken.None);

        await using var verificationConnection = await OpenWithForeignKeysAsync(database.Path);
        await using var verificationCommand = verificationConnection.CreateCommand();
        verificationCommand.CommandText = """
            SELECT
                (SELECT COUNT(*) FROM ExtractedTexts WHERE AttachmentId = $attachmentId),
                (SELECT COUNT(*) FROM SearchIndex WHERE SearchIndex MATCH 'Prüftext'),
                (SELECT COUNT(*) FROM DocumentSearchIndex WHERE DocumentSearchIndex MATCH 'Prüftext');
            """;
        verificationCommand.Parameters.AddWithValue("$attachmentId", attachment.Id.ToString("D"));
        await using var reader = await verificationCommand.ExecuteReaderAsync(CancellationToken.None);
        Assert.True(await reader.ReadAsync(CancellationToken.None));
        Assert.Equal(1, reader.GetInt32(0));
        Assert.Equal(1, reader.GetInt32(1));
        Assert.Equal(1, reader.GetInt32(2));
    }

    [Fact]
    public async Task SaveAsync_RollsBackAggregateWhenAConstraintFails()
    {
        await using var database = new TemporaryDatabase();
        var repository = new SqliteProjectRepository();
        var project = TimelineProject.Create(Guid.NewGuid(), "Rollback-Test", BaseTime);
        var first = TimelineEvent.Create(Guid.NewGuid(), "A", EventDate.Year(2020), BaseTime);
        var second = TimelineEvent.Create(Guid.NewGuid(), "B", EventDate.Year(2021), BaseTime);
        first.AddAttachment(CreateAttachment(Guid.NewGuid(), "attachments/gleich.pdf"), BaseTime.AddMinutes(1));
        second.AddAttachment(CreateAttachment(Guid.NewGuid(), "attachments/gleich.pdf"), BaseTime.AddMinutes(1));
        project.AddEvent(first, BaseTime.AddMinutes(2));
        project.AddEvent(second, BaseTime.AddMinutes(2));

        await Assert.ThrowsAsync<SqliteException>(() =>
            repository.SaveAsync(project, database.Path, CancellationToken.None));

        await using var connection = await OpenWithForeignKeysAsync(database.Path);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Projects;";
        Assert.Equal(0, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task SaveAsync_RemovesDeletedEventAndDependentRows()
    {
        await using var database = new TemporaryDatabase();
        var repository = new SqliteProjectRepository();
        var project = CreateRichProject();
        await repository.SaveAsync(project, database.Path, CancellationToken.None);
        var removed = project.Events.Single(item => item.Title == "Datum mit Uhrzeit");

        project.RemoveEvent(removed.Id, BaseTime.AddHours(3));
        await repository.SaveAsync(project, database.Path, CancellationToken.None);

        var loaded = await repository.LoadAsync(database.Path, CancellationToken.None);
        Assert.Equal(4, loaded.Events.Count);
        Assert.DoesNotContain(loaded.Events, item => item.Id == removed.Id);
        Assert.Empty(loaded.LayoutPositions);

        await using var connection = await OpenWithForeignKeysAsync(database.Path);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                (SELECT COUNT(*) FROM Attachments WHERE EventId = $eventId),
                (SELECT COUNT(*) FROM Deadlines WHERE EventId = $eventId),
                (SELECT COUNT(*) FROM EventTags WHERE EventId = $eventId),
                (SELECT COUNT(*) FROM SearchIndex WHERE EventId = $eventId);
            """;
        command.Parameters.AddWithValue("$eventId", removed.Id.ToString("D"));
        await using var reader = await command.ExecuteReaderAsync(CancellationToken.None);
        Assert.True(await reader.ReadAsync(CancellationToken.None));
        Assert.Equal(0, reader.GetInt32(0));
        Assert.Equal(0, reader.GetInt32(1));
        Assert.Equal(0, reader.GetInt32(2));
        Assert.Equal(0, reader.GetInt32(3));
    }

    [Fact]
    public async Task InitializeAsync_RejectsDatabaseFromNewerSchemaVersion()
    {
        await using var database = new TemporaryDatabase();
        var repository = new SqliteProjectRepository();
        await repository.InitializeAsync(database.Path, CancellationToken.None);

        await using (var connection = await OpenWithForeignKeysAsync(database.Path))
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                INSERT INTO SchemaMigrations (Version, Name, AppliedAtUtc)
                VALUES (999, 'Zukünftiges Schema', $timestamp);
                """;
            command.Parameters.AddWithValue("$timestamp", BaseTime.ToString("O"));
            await command.ExecuteNonQueryAsync(CancellationToken.None);
        }

        var error = await Assert.ThrowsAsync<InvalidDataException>(() =>
            repository.InitializeAsync(database.Path, CancellationToken.None));
        Assert.Contains("höchstens", error.Message, StringComparison.Ordinal);
    }

    private static TimelineProject CreateRichProject()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Beispielprojekt", BaseTime);
        project.UpdateInformation(
            "Beispielprojekt",
            "Untertitel",
            "Kurztext",
            "Ausführliche Projektbeschreibung",
            new DateOnly(1990, 1, 1),
            new DateOnly(2030, 12, 31),
            BaseTime.AddMinutes(1));
        project.ChangeSettings(
            new ProjectSettings
            {
                PreferredOrientation = TimelineOrientation.Vertical,
                Theme = ApplicationTheme.Dark,
                DefaultEventColorHex = "#112233",
                TimelineCardFontSize = 15,
                TimelineAxisFontSize = 13,
                ExportFontSize = 11,
                CompressLargeGaps = false,
                AutoSaveIntervalSeconds = 90,
                CurrentDayBackupCount = 8,
                DailyBackupCount = 14,
                WeeklyBackupCount = 12,
            },
            BaseTime.AddMinutes(2));

        var year = TimelineEvent.Create(Guid.NewGuid(), "Nur Jahr", EventDate.Year(1990), BaseTime);
        var month = TimelineEvent.Create(
            Guid.NewGuid(),
            "Monat und Jahr",
            EventDate.MonthAndYear(2024, 5),
            BaseTime.AddMinutes(1));
        var exact = TimelineEvent.Create(
            Guid.NewGuid(),
            "Exaktes Datum",
            EventDate.Exact(new DateOnly(2024, 5, 3)),
            BaseTime.AddMinutes(2));
        var exactTime = TimelineEvent.Create(
            Guid.NewGuid(),
            "Datum mit Uhrzeit",
            EventDate.ExactWithTime(new DateOnly(2024, 5, 3), new TimeOnly(9, 30)),
            BaseTime.AddMinutes(3));
        exactTime.UpdateContent(
            "Datum mit Uhrzeit",
            "Info",
            "Beschreibung mit Planung",
            "Lokale Quelle",
            "Notiz",
            BaseTime.AddMinutes(4));
        exactTime.SetClassification(EventPriority.High, EventStatus.Active, "#aabbcc", BaseTime.AddMinutes(5));
        exactTime.SetManualSortPosition(12.5m, BaseTime.AddMinutes(6));
        exactTime.SetDeadline(
            new Deadline(
                Guid.NewGuid(),
                new DateOnly(2024, 6, 1),
                new TimeOnly(12, 0),
                "Abgabe",
                DeadlineStatus.Open,
                "Vorher prüfen"),
            BaseTime.AddMinutes(7));
        exactTime.AddTag("Planung", BaseTime.AddMinutes(8));
        exactTime.AddTag("Test", BaseTime.AddMinutes(9));
        exactTime.AddAttachment(
            CreateAttachment(Guid.NewGuid(), $"attachments/{Guid.NewGuid():N}/beispiel.pdf"),
            BaseTime.AddMinutes(10));
        exactTime.AddWebLink(
            new WebLink(Guid.NewGuid(), new Uri("https://example.invalid/lokal"), "Beispiel"),
            BaseTime.AddMinutes(11));

        var range = TimelineEvent.Create(
            Guid.NewGuid(),
            "Zeitraum",
            EventDate.Range(new DateOnly(2025, 1, 2), new DateOnly(2025, 2, 3)),
            BaseTime.AddMinutes(4));

        project.AddEvent(year, BaseTime.AddMinutes(20));
        project.AddEvent(month, BaseTime.AddMinutes(21));
        project.AddEvent(exact, BaseTime.AddMinutes(22));
        project.AddEvent(exactTime, BaseTime.AddMinutes(23));
        project.AddEvent(range, BaseTime.AddMinutes(24));
        project.SetLayoutPosition(
            new LayoutPosition(exactTime.Id, TimelineOrientation.Horizontal, 17.25, -8.5),
            BaseTime.AddMinutes(25));
        return project;
    }

    private static Attachment CreateAttachment(Guid id, string projectPath) => new(
        id,
        "beispiel.pdf",
        "application/pdf",
        42,
        new string('a', 64),
        "C:\\Quelle\\beispiel.pdf",
        BaseTime,
        projectPath,
        AttachmentState.Ready,
        2);

    private static async Task<SqliteConnection> OpenWithForeignKeysAsync(string databasePath)
    {
        var connection = new SqliteConnection($"Data Source={databasePath}");
        await connection.OpenAsync(CancellationToken.None);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        await command.ExecuteNonQueryAsync(CancellationToken.None);
        return connection;
    }

    private sealed class TemporaryDatabase : IAsyncDisposable
    {
        private readonly string directory = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "ZeitstrahlStudio.Tests",
            Guid.NewGuid().ToString("N"));

        public string Path => System.IO.Path.Combine(directory, "project.db");

        public ValueTask DisposeAsync()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
