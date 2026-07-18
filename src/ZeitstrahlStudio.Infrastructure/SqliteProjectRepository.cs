using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.Infrastructure;

/// <summary>Transaktionale SQLite-Persistenz eines vollständigen Projektaggregats.</summary>
public sealed partial class SqliteProjectRepository : IProjectRepository
{
    private readonly SqliteSchemaMigrator migrator;

    /// <summary>Initialisiert das Repository.</summary>
    public SqliteProjectRepository(SqliteSchemaMigrator? migrator = null)
    {
        this.migrator = migrator ?? new SqliteSchemaMigrator();
    }

    /// <inheritdoc />
    public Task InitializeAsync(string databasePath, CancellationToken cancellationToken) =>
        migrator.MigrateAsync(databasePath, cancellationToken);

    /// <inheritdoc />
    public async Task SaveAsync(
        TimelineProject project,
        string databasePath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        project.Settings.Validate();

        await InitializeAsync(databasePath, cancellationToken).ConfigureAwait(false);
        await using var connection = await SqliteConnectionFactory
            .OpenAsync(databasePath, cancellationToken)
            .ConfigureAwait(false);
        await using var transaction = (SqliteTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        await PrepareStagingTablesAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
        await UpsertProjectAsync(connection, transaction, project, cancellationToken).ConfigureAwait(false);
        await UpsertSettingsAsync(connection, transaction, project, cancellationToken).ConfigureAwait(false);

        foreach (var timelineEvent in project.Events)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await StageIdAsync(connection, transaction, "SaveEventIds", timelineEvent.Id, cancellationToken)
                .ConfigureAwait(false);
            await UpsertEventAsync(connection, transaction, project.Id, timelineEvent, cancellationToken)
                .ConfigureAwait(false);
            await UpsertEventDateAsync(connection, transaction, timelineEvent, cancellationToken)
                .ConfigureAwait(false);
            await SynchronizeDeadlineAsync(connection, transaction, timelineEvent, cancellationToken)
                .ConfigureAwait(false);
            await SynchronizeTagsAsync(connection, transaction, timelineEvent, cancellationToken)
                .ConfigureAwait(false);
            await UpsertAttachmentsAsync(connection, transaction, timelineEvent, cancellationToken)
                .ConfigureAwait(false);
            await UpsertWebLinksAsync(connection, transaction, timelineEvent, cancellationToken)
                .ConfigureAwait(false);
        }

        await SynchronizeLayoutPositionsAsync(connection, transaction, project, cancellationToken)
            .ConfigureAwait(false);
        await DeleteRemovedItemsAsync(connection, transaction, project.Id, cancellationToken)
            .ConfigureAwait(false);
        await RefreshSearchIndexAsync(connection, transaction, project.Id, cancellationToken)
            .ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TimelineProject> LoadAsync(string databasePath, CancellationToken cancellationToken)
    {
        await InitializeAsync(databasePath, cancellationToken).ConfigureAwait(false);
        await using var connection = await SqliteConnectionFactory
            .OpenAsync(databasePath, cancellationToken)
            .ConfigureAwait(false);
        return await LoadProjectAsync(connection, cancellationToken).ConfigureAwait(false);
    }

    private static SqliteCommand CreateCommand(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string commandText)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        return command;
    }

    private static void AddParameter(SqliteCommand command, string name, object? value) =>
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
}
