using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Infrastructure;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class ApplicationStateRecoveryAndLoggingTests
{
    private static readonly DateTimeOffset BaseTime = new(2026, 7, 19, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RecentProjectsService_DeduplicatesMarksMissingAndRemovesEntries()
    {
        await using var root = new TemporaryRoot();
        var statePath = System.IO.Path.Combine(root.Path, "state", "application-state.json");
        using var service = new JsonRecentProjectsService(statePath, new FixedTimeProvider(BaseTime));
        var existingArchive = System.IO.Path.Combine(root.Path, "Vorhanden.zeitprojekt");
        await File.WriteAllBytesAsync(existingArchive, Array.Empty<byte>());
        var missingArchive = System.IO.Path.Combine(root.Path, "Fehlt.zeitprojekt");

        await service.RecordOpenedAsync(CreateWorkspace("Vorhanden", existingArchive), CancellationToken.None);
        await service.RecordOpenedAsync(CreateWorkspace("Umbenannt", existingArchive), CancellationToken.None);
        await service.RecordOpenedAsync(CreateWorkspace("Fehlt", missingArchive), CancellationToken.None);

        var entries = await service.GetAsync(CancellationToken.None);
        Assert.Equal(2, entries.Count);
        Assert.Single(entries, entry => entry.ProjectName == "Umbenannt" && entry.FileExists);
        Assert.Single(entries, entry => entry.ProjectName == "Fehlt" && !entry.FileExists);

        await service.RemoveMissingAsync(CancellationToken.None);
        Assert.Single(await service.GetAsync(CancellationToken.None));
        await service.RemoveAsync(existingArchive, CancellationToken.None);
        Assert.Empty(await service.GetAsync(CancellationToken.None));
        Assert.True(File.Exists(statePath));
    }

    [Fact]
    public async Task RecoveryService_ExcludesActiveWorkspaceAndRecoversOrphan()
    {
        await using var root = new TemporaryRoot();
        var workspaceRoot = System.IO.Path.Combine(root.Path, "workspaces");
        var archivePath = System.IO.Path.Combine(root.Path, "Recovery.zeitprojekt");
        var repository = new SqliteProjectRepository();
        var archiveService = new ProjectArchiveService(repository, new FixedTimeProvider(BaseTime));
        var service = new LocalProjectWorkspaceService(
            repository,
            archiveService,
            workspaceRoot,
            new FixedTimeProvider(BaseTime));
        var workspace = await service.CreateAsync("Recovery", archivePath, CancellationToken.None);

        Assert.Empty(await service.FindAsync(CancellationToken.None));

        File.Delete(System.IO.Path.Combine(workspace.WorkingDirectory, "metadata", "session.json"));
        var candidate = Assert.Single(await service.FindAsync(CancellationToken.None));
        Assert.Equal(workspace.Project.Id, candidate.ProjectId);

        var recovered = await service.RecoverAsync(candidate, CancellationToken.None);
        Assert.True(recovered.HasUnsavedChanges);
        Assert.True(File.Exists(System.IO.Path.Combine(recovered.WorkingDirectory, "metadata", "session.json")));
        await service.DiscardAsync(candidate, CancellationToken.None);
        Assert.False(Directory.Exists(workspace.WorkingDirectory));
        Assert.True(File.Exists(archivePath));
    }

    [Fact]
    public async Task AutosaveService_SavesDirtyWorkspaceAndUpdatesCaller()
    {
        await using var root = new TemporaryRoot();
        var repository = new SqliteProjectRepository();
        var fakeTime = new ImmediateTimerTimeProvider(BaseTime);
        var workspaceService = new LocalProjectWorkspaceService(
            repository,
            new ProjectArchiveService(repository, fakeTime),
            System.IO.Path.Combine(root.Path, "workspaces"),
            fakeTime);
        var archivePath = System.IO.Path.Combine(root.Path, "Autosave.zeitprojekt");
        var current = await workspaceService.CreateAsync("Autosave", archivePath, CancellationToken.None);
        current.Project.AddEvent(
            TimelineEvent.Create(Guid.NewGuid(), "Autosave-Ereignis", EventDate.Year(2026), BaseTime),
            BaseTime.AddMinutes(1));
        current = current with { HasUnsavedChanges = true };
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var autosave = new ProjectAutosaveService(workspaceService, fakeTime);
        var progress = new SynchronousProgress<AutosaveStatus>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => autosave.RunAsync(
            () => current,
            updated =>
            {
                current = updated;
                cancellation.Cancel();
            },
            TimeSpan.FromSeconds(15),
            progress,
            cancellation.Token));

        Assert.False(current.HasUnsavedChanges);
        Assert.Contains(progress.Values, item => item.Succeeded);
        var reopened = await workspaceService.OpenAsync(archivePath, CancellationToken.None);
        Assert.Single(reopened.Project.Events);
        await workspaceService.CloseAsync(reopened, CancellationToken.None);
        await workspaceService.CloseAsync(current, CancellationToken.None);
    }

    [Fact]
    public async Task AutosaveService_CreatesDueAutomaticBackupAfterSuccessfulSave()
    {
        await using var root = new TemporaryRoot();
        var repository = new SqliteProjectRepository();
        var fakeTime = new ImmediateTimerTimeProvider(BaseTime);
        var workspaceService = new LocalProjectWorkspaceService(
            repository,
            new ProjectArchiveService(repository, fakeTime),
            System.IO.Path.Combine(root.Path, "workspaces"),
            fakeTime);
        var archivePath = System.IO.Path.Combine(root.Path, "Autosave-mit-Sicherung.zeitprojekt");
        var current = await workspaceService.CreateAsync(
            "Autosave mit Sicherung",
            archivePath,
            CancellationToken.None);
        current.Project.AddEvent(
            TimelineEvent.Create(Guid.NewGuid(), "Zu sichern", EventDate.Year(2026), BaseTime),
            BaseTime.AddMinutes(1));
        current = current with { HasUnsavedChanges = true };
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var backups = new RecordingBackupService(() => cancellation.Cancel());
        using var autosave = new ProjectAutosaveService(workspaceService, backups, fakeTime);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => autosave.RunAsync(
            () => current,
            updated => current = updated,
            TimeSpan.FromSeconds(15),
            progress: null,
            cancellation.Token));

        Assert.False(current.HasUnsavedChanges);
        Assert.Equal(1, backups.AutomaticRequests);
        await workspaceService.CloseAsync(current, CancellationToken.None);
    }

    [Fact]
    public async Task LocalLogService_RotatesReadsExportsAndClearsJsonLines()
    {
        await using var root = new TemporaryRoot();
        var logDirectory = System.IO.Path.Combine(root.Path, "logs");
        using var service = new JsonLinesLocalLogService(logDirectory, 64 * 1024, 3);
        for (var index = 0; index < 10; index++)
        {
            await service.WriteAsync(
                new LocalLogEntry(
                    BaseTime.AddSeconds(index),
                    LocalLogLevel.Information,
                    "Integrationstest",
                    $"Ereignis-{index}",
                    new string((char)('a' + index), 10_000)),
                CancellationToken.None);
        }

        Assert.True(File.Exists(System.IO.Path.Combine(logDirectory, "application.log.jsonl.1")));
        var recent = await service.ReadRecentAsync(3, CancellationToken.None);
        Assert.Equal(new[] { "Ereignis-9", "Ereignis-8", "Ereignis-7" }, recent.Select(item => item.EventName));

        var exportPath = System.IO.Path.Combine(root.Path, "export", "logs.jsonl");
        await service.ExportAsync(exportPath, CancellationToken.None);
        Assert.Contains("Ereignis-9", await File.ReadAllTextAsync(exportPath), StringComparison.Ordinal);

        await service.ClearAsync(CancellationToken.None);
        Assert.Empty(Directory.EnumerateFiles(logDirectory));
        Assert.True(File.Exists(exportPath));
    }

    private static ProjectWorkspace CreateWorkspace(string projectName, string archivePath) => new(
        TimelineProject.Create(Guid.NewGuid(), projectName, BaseTime),
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N")),
        archivePath,
        HasUnsavedChanges: false);

    private sealed class FixedTimeProvider(DateTimeOffset timestamp) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => timestamp;
    }

    private sealed class RecordingBackupService(Action onAutomaticRequest) : IBackupService
    {
        public int AutomaticRequests { get; private set; }

        public Task<BackupRecord> CreateAsync(
            ProjectWorkspace workspace,
            bool automatic,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<BackupRecord?> CreateAutomaticIfDueAsync(
            ProjectWorkspace workspace,
            CancellationToken cancellationToken)
        {
            AutomaticRequests++;
            onAutomaticRequest();
            return Task.FromResult<BackupRecord?>(new BackupRecord(
                Guid.NewGuid(),
                BaseTime,
                "project/automatic.zeitprojekt",
                1,
                new string('a', 64),
                true));
        }

        public Task<IReadOnlyList<BackupRecord>> ListAsync(
            ProjectWorkspace workspace,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ProjectWorkspace> RestoreAsync(
            ProjectWorkspace currentWorkspace,
            BackupRecord backup,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task ApplyRetentionAsync(
            ProjectWorkspace workspace,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class ImmediateTimerTimeProvider(DateTimeOffset timestamp) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => timestamp;

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period) => new ImmediateTimer(callback, state);
    }

    private sealed class ImmediateTimer : ITimer
    {
        private readonly TimerCallback callback;
        private readonly object? state;
        private int disposed;

        public ImmediateTimer(TimerCallback callback, object? state)
        {
            this.callback = callback;
            this.state = state;
            ThreadPool.QueueUserWorkItem(_ => Fire());
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            ThreadPool.QueueUserWorkItem(_ => Fire());
            return Volatile.Read(ref disposed) == 0;
        }

        public void Dispose() => Interlocked.Exchange(ref disposed, 1);

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        private void Fire()
        {
            if (Volatile.Read(ref disposed) == 0)
            {
                callback(state);
            }
        }
    }

    private sealed class SynchronousProgress<T> : IProgress<T>
    {
        public List<T> Values { get; } = [];
        public void Report(T value) => Values.Add(value);
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
