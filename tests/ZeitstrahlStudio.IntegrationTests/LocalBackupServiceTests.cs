using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Infrastructure;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class LocalBackupServiceTests
{
    private static readonly DateTimeOffset BaseTime =
        new(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAndRestore_PreserveDirtyStateValidateHashAndWriteAudit()
    {
        await using var root = new TemporaryRoot();
        var context = await CreateContextAsync(root, BaseTime);
        var eventId = Guid.NewGuid();
        context.Workspace.Project.AddEvent(
            TimelineEvent.Create(eventId, "Vor der Sicherung", EventDate.Year(2025), BaseTime),
            BaseTime.AddMinutes(1));
        context.Workspace = context.Workspace with { HasUnsavedChanges = true };
        context.Time.SetUtcNow(BaseTime.AddMinutes(2));

        var backup = await context.Backups.CreateAsync(
            context.Workspace,
            automatic: false,
            CancellationToken.None);

        Assert.False(backup.IsAutomatic);
        Assert.Equal(64, backup.Sha256.Length);
        Assert.True(context.Workspace.HasUnsavedChanges);
        var backupPath = Path.Combine(
            context.BackupRoot,
            backup.RelativeArchivePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(backupPath));
        Assert.Equal(
            backup.Sha256,
            Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(backupPath))).ToLowerInvariant());
        Assert.Equal(backup, Assert.Single(await context.Backups.ListAsync(
            context.Workspace,
            CancellationToken.None)));

        context.Workspace.Project.RemoveEvent(eventId, BaseTime.AddMinutes(3));
        context.Workspace = context.Workspace with { HasUnsavedChanges = true };
        context.Time.SetUtcNow(BaseTime.AddMinutes(4));
        var restored = await context.Backups.RestoreAsync(
            context.Workspace,
            backup,
            CancellationToken.None);

        Assert.True(restored.HasUnsavedChanges);
        Assert.Equal(context.Workspace.ArchivePath, restored.ArchivePath);
        Assert.Equal("Vor der Sicherung", Assert.Single(restored.Project.Events).Title);
        var restoredAudit = await context.Audit.ReadAsync(restored, CancellationToken.None);
        Assert.Contains(restoredAudit, entry => entry.Operation == "BackupRestore" && entry.Succeeded);
        var allBackups = await context.Backups.ListAsync(restored, CancellationToken.None);
        Assert.Contains(allBackups, item => item.Id == backup.Id);
        Assert.True(allBackups.Count(item => !item.IsAutomatic) >= 2);

        await context.Workspaces.CloseAsync(restored, CancellationToken.None);
        await context.Workspaces.CloseAsync(context.Workspace, CancellationToken.None);
        context.Backups.Dispose();
    }

    [Fact]
    public async Task Restore_RejectsChangedBackupBeforeAllocatingWorkspace()
    {
        await using var root = new TemporaryRoot();
        var context = await CreateContextAsync(root, BaseTime);
        var backup = await context.Backups.CreateAsync(
            context.Workspace,
            automatic: false,
            CancellationToken.None);
        var backupPath = Path.Combine(
            context.BackupRoot,
            backup.RelativeArchivePath.Replace('/', Path.DirectorySeparatorChar));
        await File.AppendAllTextAsync(backupPath, "manipuliert");
        var workspaceCountBefore = Directory.EnumerateDirectories(context.WorkspaceRoot).Count();

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            context.Backups.RestoreAsync(context.Workspace, backup, CancellationToken.None));

        Assert.Contains("Größe", exception.Message, StringComparison.Ordinal);
        Assert.Equal(workspaceCountBefore, Directory.EnumerateDirectories(context.WorkspaceRoot).Count());
        Assert.Single(Directory.EnumerateFiles(
            Path.Combine(context.BackupRoot, context.Workspace.Project.Id.ToString("N")),
            "*.zeitprojekt"));
        await context.Workspaces.CloseAsync(context.Workspace, CancellationToken.None);
        context.Backups.Dispose();
    }

    [Fact]
    public async Task List_ReconstructsMetadataForCompletedOrphanArchive()
    {
        await using var root = new TemporaryRoot();
        var context = await CreateContextAsync(root, BaseTime);
        var backup = await context.Backups.CreateAsync(
            context.Workspace,
            automatic: false,
            CancellationToken.None);
        await using (var connection = new SqliteConnection(
                         $"Data Source={Path.Combine(context.Workspace.WorkingDirectory, "project.db")}"))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Backups WHERE Id = $id;";
            command.Parameters.AddWithValue("$id", backup.Id.ToString("D"));
            Assert.Equal(1, await command.ExecuteNonQueryAsync());
        }

        var reconstructed = Assert.Single(await context.Backups.ListAsync(
            context.Workspace,
            CancellationToken.None));

        Assert.Equal(backup, reconstructed);
        await context.Workspaces.CloseAsync(context.Workspace, CancellationToken.None);
        context.Backups.Dispose();
    }

    [Fact]
    public async Task AutomaticBackups_RotateByCurrentDayDailyAndWeeklyWhileManualRemains()
    {
        await using var root = new TemporaryRoot();
        var context = await CreateContextAsync(root, BaseTime);
        context.Workspace.Project.ChangeSettings(
            context.Workspace.Project.Settings with
            {
                CurrentDayBackupCount = 2,
                DailyBackupCount = 2,
                WeeklyBackupCount = 2,
            },
            BaseTime);
        context.Workspace = context.Workspace with { HasUnsavedChanges = true };

        context.Time.SetUtcNow(new DateTimeOffset(2026, 1, 2, 8, 0, 0, TimeSpan.Zero));
        var manual = await context.Backups.CreateAsync(context.Workspace, false, CancellationToken.None);
        foreach (var timestamp in new[]
                 {
                     new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.Zero),
                     new DateTimeOffset(2026, 1, 8, 9, 0, 0, TimeSpan.Zero),
                     new DateTimeOffset(2026, 1, 13, 9, 0, 0, TimeSpan.Zero),
                     new DateTimeOffset(2026, 1, 14, 9, 0, 0, TimeSpan.Zero),
                     new DateTimeOffset(2026, 1, 14, 12, 0, 0, TimeSpan.Zero),
                     new DateTimeOffset(2026, 1, 15, 8, 0, 0, TimeSpan.Zero),
                     new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.Zero),
                     new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero),
                 })
        {
            context.Time.SetUtcNow(timestamp);
            await context.Backups.CreateAsync(context.Workspace, true, CancellationToken.None);
        }

        var retained = await context.Backups.ListAsync(context.Workspace, CancellationToken.None);
        Assert.Contains(retained, item => item.Id == manual.Id && !item.IsAutomatic);
        Assert.Equal(6, retained.Count(item => item.IsAutomatic));
        Assert.Equal(7, retained.Count);
        var automaticDates = retained
            .Where(item => item.IsAutomatic)
            .Select(item => item.CreatedAtUtc.UtcDateTime.Date)
            .ToArray();
        Assert.Equal(2, automaticDates.Count(date => date == new DateTime(2026, 1, 15)));
        Assert.Single(automaticDates, date => date == new DateTime(2026, 1, 14));
        Assert.Single(automaticDates, date => date == new DateTime(2026, 1, 13));
        Assert.Single(automaticDates, date => date == new DateTime(2026, 1, 8));
        Assert.Single(automaticDates, date => date == new DateTime(2026, 1, 1));

        Assert.Null(await context.Backups.CreateAutomaticIfDueAsync(
            context.Workspace,
            CancellationToken.None));
        context.Time.SetUtcNow(new DateTimeOffset(2026, 1, 15, 22, 0, 0, TimeSpan.Zero));
        Assert.NotNull(await context.Backups.CreateAutomaticIfDueAsync(
            context.Workspace,
            CancellationToken.None));
        await context.Workspaces.CloseAsync(context.Workspace, CancellationToken.None);
        context.Backups.Dispose();
    }

    private static async Task<TestContext> CreateContextAsync(
        TemporaryRoot root,
        DateTimeOffset timestamp)
    {
        var workspaceRoot = Path.Combine(root.Path, "workspaces");
        var backupRoot = Path.Combine(root.Path, "backups");
        var archiveRoot = Path.Combine(root.Path, "archives");
        Directory.CreateDirectory(archiveRoot);
        var time = new MutableTimeProvider(timestamp);
        var repository = new SqliteProjectRepository();
        var archive = new ProjectArchiveService(repository, time);
        var workspaces = new LocalProjectWorkspaceService(repository, archive, workspaceRoot, time);
        var audit = new SqliteAuditLogService();
        var backups = new LocalBackupService(
            workspaces,
            audit,
            new BackupRetentionPolicy(),
            logService: null,
            backupRoot,
            time,
            TimeZoneInfo.Utc);
        var workspace = await workspaces.CreateAsync(
            "Sicherungstest",
            Path.Combine(archiveRoot, "Sicherungstest.zeitprojekt"),
            CancellationToken.None);
        return new TestContext(
            workspaces,
            audit,
            backups,
            time,
            workspaceRoot,
            backupRoot,
            workspace);
    }

    private sealed class MutableTimeProvider(DateTimeOffset timestamp) : TimeProvider
    {
        private DateTimeOffset timestamp = timestamp;
        public override DateTimeOffset GetUtcNow() => timestamp;
        public void SetUtcNow(DateTimeOffset value) => timestamp = value;
    }

    private sealed class TestContext(
        LocalProjectWorkspaceService workspaces,
        SqliteAuditLogService audit,
        LocalBackupService backups,
        MutableTimeProvider time,
        string workspaceRoot,
        string backupRoot,
        ProjectWorkspace workspace)
    {
        public LocalProjectWorkspaceService Workspaces { get; } = workspaces;
        public SqliteAuditLogService Audit { get; } = audit;
        public LocalBackupService Backups { get; } = backups;
        public MutableTimeProvider Time { get; } = time;
        public string WorkspaceRoot { get; } = workspaceRoot;
        public string BackupRoot { get; } = backupRoot;
        public ProjectWorkspace Workspace { get; set; } = workspace;
    }

    private sealed class TemporaryRoot : IAsyncDisposable
    {
        public TemporaryRoot()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "ZeitstrahlStudio.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public ValueTask DisposeAsync()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
