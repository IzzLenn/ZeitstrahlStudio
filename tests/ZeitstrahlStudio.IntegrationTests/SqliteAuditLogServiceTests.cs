using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Infrastructure;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class SqliteAuditLogServiceTests
{
    private static readonly DateTimeOffset BaseTime =
        new(2026, 7, 19, 11, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task WriteAndReadAsync_PreservesEntryAndSortsNewestFirst()
    {
        await using var context = await AuditTestContext.CreateAsync();
        var service = new SqliteAuditLogService();
        var first = new AuditEntry(
            Guid.NewGuid(),
            BaseTime,
            "Create",
            "TimelineEvent",
            Guid.NewGuid(),
            "Ereignis erstellt",
            true,
            null);
        var second = new AuditEntry(
            Guid.NewGuid(),
            BaseTime.AddMinutes(1),
            "Update",
            "TimelineEvent",
            first.EntityId,
            "Ereignis bearbeitet",
            false,
            "Validierungsfehler");

        await service.WriteAsync(context.Workspace, first, CancellationToken.None);
        await service.WriteAsync(context.Workspace, second, CancellationToken.None);
        var entries = await service.ReadAsync(context.Workspace, CancellationToken.None);

        Assert.Equal([second, first], entries);
    }

    [Fact]
    public async Task RepositorySave_PreservesExistingAuditRows()
    {
        await using var context = await AuditTestContext.CreateAsync();
        var service = new SqliteAuditLogService();
        var entry = new AuditEntry(
            Guid.NewGuid(),
            BaseTime,
            "Create",
            "TimelineEvent",
            Guid.NewGuid(),
            "Ereignis erstellt",
            true,
            null);
        await service.WriteAsync(context.Workspace, entry, CancellationToken.None);

        context.Workspace.Project.UpdateInformation(
            "Geänderte Chronik",
            null,
            null,
            null,
            null,
            null,
            BaseTime.AddMinutes(2));
        await context.Repository.SaveAsync(
            context.Workspace.Project,
            context.DatabasePath,
            CancellationToken.None);

        Assert.Equal([entry], await service.ReadAsync(context.Workspace, CancellationToken.None));
    }

    private sealed class AuditTestContext : IAsyncDisposable
    {
        private readonly string directory;

        private AuditTestContext(
            string directory,
            SqliteProjectRepository repository,
            ProjectWorkspace workspace)
        {
            this.directory = directory;
            Repository = repository;
            Workspace = workspace;
        }

        public SqliteProjectRepository Repository { get; }
        public ProjectWorkspace Workspace { get; }
        public string DatabasePath => Path.Combine(directory, "project.db");

        public static async Task<AuditTestContext> CreateAsync()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "ZeitstrahlStudio.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var repository = new SqliteProjectRepository();
            var project = TimelineProject.Create(Guid.NewGuid(), "Chronik", BaseTime);
            var databasePath = Path.Combine(directory, "project.db");
            await repository.SaveAsync(project, databasePath, CancellationToken.None);
            return new AuditTestContext(
                directory,
                repository,
                new ProjectWorkspace(project, directory, null, HasUnsavedChanges: false));
        }

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
