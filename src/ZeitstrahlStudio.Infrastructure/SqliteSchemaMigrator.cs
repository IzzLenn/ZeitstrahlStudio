using System.Globalization;
using Microsoft.Data.Sqlite;

namespace ZeitstrahlStudio.Infrastructure;

/// <summary>Erstellt und migriert das normalisierte SQLite-Projektschema.</summary>
public sealed class SqliteSchemaMigrator
{
    /// <summary>Aktuell von dieser Anwendung unterstützte Schema-Version.</summary>
    public const int CurrentVersion = 1;

    private const string MigrationOneSql = """
        CREATE TABLE Projects (
            Id TEXT NOT NULL PRIMARY KEY,
            Name TEXT NOT NULL,
            Subtitle TEXT NULL,
            InfoText TEXT NULL,
            Description TEXT NULL,
            OverallStart TEXT NULL,
            OverallEnd TEXT NULL,
            CreatedAtUtc TEXT NOT NULL,
            ModifiedAtUtc TEXT NOT NULL
        );

        CREATE TABLE Events (
            Id TEXT NOT NULL PRIMARY KEY,
            ProjectId TEXT NOT NULL,
            Title TEXT NOT NULL,
            InfoText TEXT NULL,
            Description TEXT NULL,
            Priority INTEGER NOT NULL,
            ColorHex TEXT NOT NULL,
            Source TEXT NULL,
            Notes TEXT NULL,
            Status INTEGER NOT NULL,
            ManualSortPosition TEXT NULL,
            CreatedAtUtc TEXT NOT NULL,
            ModifiedAtUtc TEXT NOT NULL,
            FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE ON UPDATE CASCADE
        );

        CREATE TABLE EventDates (
            EventId TEXT NOT NULL PRIMARY KEY,
            Precision INTEGER NOT NULL CHECK (Precision BETWEEN 0 AND 4),
            StartYear INTEGER NOT NULL CHECK (StartYear BETWEEN 1 AND 9999),
            StartMonth INTEGER NULL CHECK (StartMonth IS NULL OR StartMonth BETWEEN 1 AND 12),
            StartDay INTEGER NULL CHECK (StartDay IS NULL OR StartDay BETWEEN 1 AND 31),
            StartTime TEXT NULL,
            EndYear INTEGER NULL CHECK (EndYear IS NULL OR EndYear BETWEEN 1 AND 9999),
            EndMonth INTEGER NULL CHECK (EndMonth IS NULL OR EndMonth BETWEEN 1 AND 12),
            EndDay INTEGER NULL CHECK (EndDay IS NULL OR EndDay BETWEEN 1 AND 31),
            FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE
        );

        CREATE TABLE Deadlines (
            Id TEXT NOT NULL PRIMARY KEY,
            EventId TEXT NOT NULL UNIQUE,
            DueDate TEXT NOT NULL,
            DueTime TEXT NULL,
            Label TEXT NULL,
            Status INTEGER NOT NULL,
            ReminderNote TEXT NULL,
            FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE
        );

        CREATE TABLE Attachments (
            Id TEXT NOT NULL PRIMARY KEY,
            EventId TEXT NOT NULL,
            OriginalFileName TEXT NOT NULL,
            MediaType TEXT NOT NULL,
            FileSize INTEGER NOT NULL CHECK (FileSize >= 0),
            Sha256 TEXT NOT NULL CHECK (length(Sha256) = 64),
            OriginalSourcePath TEXT NULL,
            ImportedAtUtc TEXT NOT NULL,
            ProjectRelativePath TEXT NOT NULL UNIQUE,
            State INTEGER NOT NULL,
            LinkedPdfPage INTEGER NULL CHECK (LinkedPdfPage IS NULL OR LinkedPdfPage >= 1),
            FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE
        );

        CREATE TABLE AttachmentMetadata (
            AttachmentId TEXT NOT NULL,
            MetadataKey TEXT NOT NULL,
            MetadataValue TEXT NOT NULL,
            PRIMARY KEY (AttachmentId, MetadataKey),
            FOREIGN KEY (AttachmentId) REFERENCES Attachments(Id) ON DELETE CASCADE
        );

        CREATE TABLE ExtractedTexts (
            AttachmentId TEXT NOT NULL PRIMARY KEY,
            Content TEXT NOT NULL,
            ExtractionMethod INTEGER NOT NULL,
            Language TEXT NULL,
            ExtractedAtUtc TEXT NOT NULL,
            FOREIGN KEY (AttachmentId) REFERENCES Attachments(Id) ON DELETE CASCADE
        );

        CREATE TABLE WebLinks (
            Id TEXT NOT NULL PRIMARY KEY,
            EventId TEXT NOT NULL,
            Address TEXT NOT NULL,
            Label TEXT NULL,
            FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE,
            UNIQUE (EventId, Address)
        );

        CREATE TABLE Tags (
            Id TEXT NOT NULL PRIMARY KEY,
            Name TEXT NOT NULL COLLATE NOCASE UNIQUE
        );

        CREATE TABLE EventTags (
            EventId TEXT NOT NULL,
            TagId TEXT NOT NULL,
            PRIMARY KEY (EventId, TagId),
            FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE,
            FOREIGN KEY (TagId) REFERENCES Tags(Id) ON DELETE CASCADE
        );

        CREATE TABLE LayoutPositions (
            EventId TEXT NOT NULL,
            Orientation INTEGER NOT NULL,
            HorizontalOffset REAL NOT NULL,
            VerticalOffset REAL NOT NULL,
            PRIMARY KEY (EventId, Orientation),
            FOREIGN KEY (EventId) REFERENCES Events(Id) ON DELETE CASCADE
        );

        CREATE TABLE ProjectSettings (
            ProjectId TEXT NOT NULL PRIMARY KEY,
            PreferredOrientation INTEGER NOT NULL,
            Theme INTEGER NOT NULL,
            DefaultEventColorHex TEXT NOT NULL,
            TimelineCardFontSize REAL NOT NULL,
            TimelineAxisFontSize REAL NOT NULL,
            ExportFontSize REAL NOT NULL,
            CompressLargeGaps INTEGER NOT NULL,
            AutoSaveIntervalSeconds INTEGER NOT NULL,
            CurrentDayBackupCount INTEGER NOT NULL,
            DailyBackupCount INTEGER NOT NULL,
            WeeklyBackupCount INTEGER NOT NULL,
            FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE ON UPDATE CASCADE
        );

        CREATE TABLE AuditLog (
            Id TEXT NOT NULL PRIMARY KEY,
            ProjectId TEXT NOT NULL,
            TimestampUtc TEXT NOT NULL,
            Operation TEXT NOT NULL,
            EntityType TEXT NOT NULL,
            EntityId TEXT NULL,
            Description TEXT NOT NULL,
            Succeeded INTEGER NOT NULL,
            TechnicalDetails TEXT NULL,
            FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE ON UPDATE CASCADE
        );

        CREATE TABLE ApplicationLogReferences (
            Id TEXT NOT NULL PRIMARY KEY,
            ProjectId TEXT NOT NULL,
            RelativePath TEXT NOT NULL,
            CreatedAtUtc TEXT NOT NULL,
            FileSize INTEGER NOT NULL CHECK (FileSize >= 0),
            FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE ON UPDATE CASCADE
        );

        CREATE TABLE Backups (
            Id TEXT NOT NULL PRIMARY KEY,
            ProjectId TEXT NOT NULL,
            CreatedAtUtc TEXT NOT NULL,
            RelativeArchivePath TEXT NOT NULL,
            FileSize INTEGER NOT NULL CHECK (FileSize >= 0),
            Sha256 TEXT NOT NULL CHECK (length(Sha256) = 64),
            IsAutomatic INTEGER NOT NULL,
            FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE ON UPDATE CASCADE
        );

        CREATE VIRTUAL TABLE SearchIndex USING fts5(
            ProjectId UNINDEXED,
            EventId UNINDEXED,
            Content,
            tokenize = 'unicode61 remove_diacritics 2'
        );

        CREATE INDEX IX_Events_Project_DateOrder ON Events(ProjectId, ManualSortPosition, CreatedAtUtc);
        CREATE INDEX IX_Deadlines_DueDate_Status ON Deadlines(DueDate, Status);
        CREATE INDEX IX_Attachments_EventId_MediaType ON Attachments(EventId, MediaType);
        CREATE INDEX IX_EventTags_TagId ON EventTags(TagId);
        CREATE INDEX IX_AuditLog_ProjectId_TimestampUtc ON AuditLog(ProjectId, TimestampUtc DESC);
        CREATE INDEX IX_Backups_ProjectId_CreatedAtUtc ON Backups(ProjectId, CreatedAtUtc DESC);
        """;

    /// <summary>Wendet alle fehlenden Migrationen in Transaktionen an.</summary>
    public async Task MigrateAsync(string databasePath, CancellationToken cancellationToken)
    {
        await using var connection = await SqliteConnectionFactory
            .OpenAsync(databasePath, cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        await ExecuteAsync(
            connection,
            transaction,
            """
            CREATE TABLE IF NOT EXISTS SchemaMigrations (
                Version INTEGER NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                AppliedAtUtc TEXT NOT NULL
            );
            """,
            cancellationToken).ConfigureAwait(false);

        var currentVersion = await ReadCurrentVersionAsync(connection, transaction, cancellationToken)
            .ConfigureAwait(false);
        if (currentVersion > CurrentVersion)
        {
            throw new InvalidDataException(
                $"Die Projektdatenbank verwendet Schema-Version {currentVersion}. " +
                $"Diese Anwendung unterstützt höchstens Version {CurrentVersion}.");
        }

        if (currentVersion < 1)
        {
            await ExecuteAsync(connection, transaction, MigrationOneSql, cancellationToken).ConfigureAwait(false);
            await RecordMigrationAsync(
                connection,
                transaction,
                1,
                "Initiales normalisiertes Projektschema",
                cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<int> ReadCurrentVersionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COALESCE(MAX(Version), 0) FROM SchemaMigrations;";
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static async Task RecordMigrationAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int version,
        string name,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO SchemaMigrations (Version, Name, AppliedAtUtc)
            VALUES ($version, $name, $appliedAtUtc);
            """;
        command.Parameters.AddWithValue("$version", version);
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$appliedAtUtc", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
