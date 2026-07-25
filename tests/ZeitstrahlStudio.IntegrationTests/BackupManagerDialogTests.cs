using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Threading;
using ZeitstrahlStudio.App;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class BackupManagerDialogTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ViewModel_AppliesValidatedRetentionSettingsAndCheckpointsWorkspace()
    {
        var workspace = CreateWorkspace();
        var backups = new FakeBackupService(CreateBackupRecord());
        var workspaces = new FakeWorkspaceService();
        var viewModel = new BackupManagerDialogViewModel(
            backups,
            workspaces,
            workspace,
            _ => false,
            new FixedTimeProvider(Timestamp.AddMinutes(1)));
        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.CurrentDayBackupCount = 4;
        viewModel.DailyBackupCount = 14;
        viewModel.WeeklyBackupCount = 12;
        viewModel.ApplySettingsCommand.Execute(null);

        await WaitUntilAsync(
            () => viewModel.SettingsChanged && !viewModel.IsBusy,
            TimeSpan.FromSeconds(5));
        Assert.Equal(4, viewModel.Workspace.Project.Settings.CurrentDayBackupCount);
        Assert.Equal(14, viewModel.Workspace.Project.Settings.DailyBackupCount);
        Assert.Equal(12, viewModel.Workspace.Project.Settings.WeeklyBackupCount);
        Assert.True(viewModel.Workspace.HasUnsavedChanges);
        Assert.Equal(1, workspaces.CheckpointCalls);
        Assert.Equal(1, backups.RetentionCalls);
        Assert.False(viewModel.HasError);
    }

    [Fact]
    public async Task Dialog_InitializesBackupListBindingsOnStaThread()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(async () =>
        {
            try
            {
                var viewModel = new BackupManagerDialogViewModel(
                    new FakeBackupService(CreateBackupRecord()),
                    new FakeWorkspaceService(),
                    CreateWorkspace(),
                    _ => false,
                    localTimeZone: TimeZoneInfo.Utc);
                await viewModel.InitializeAsync(CancellationToken.None);

                Assert.Single(viewModel.Backups);
                Assert.Equal("Manuell", viewModel.Backups[0].TypeDisplay);
                var dialog = new BackupManagerDialog(viewModel);
                dialog.Measure(new Size(1_100, 680));
                dialog.Arrange(new Rect(0, 0, 1_100, 680));
                dialog.UpdateLayout();
                var header = Assert.IsType<Border>(dialog.FindName("BackupManagerHeader"));
                Assert.Equal("Sicherungsverwaltung: Beschreibung", AutomationProperties.GetName(header));
                var retention = Assert.IsType<ScrollViewer>(dialog.FindName("BackupRetentionPanel"));
                Assert.Equal("Aufbewahrungseinstellungen für Sicherungen", AutomationProperties.GetName(retention));
                var backupGrid = Assert.IsType<DataGrid>(dialog.FindName("BackupGrid"));
                Assert.Equal("Verfügbare Projektsicherungen", AutomationProperties.GetName(backupGrid));
                var emptyState = Assert.IsType<Border>(dialog.FindName("BackupEmptyState"));
                Assert.Equal(Visibility.Collapsed, emptyState.Visibility);
                var close = LogicalTreeHelper.GetChildren(dialog)
                    .OfType<DependencyObject>()
                    .SelectMany(FindDescendants)
                    .OfType<Button>()
                    .Single(button => AutomationProperties.GetName(button) == "Sicherungsverwaltung schließen");
                Assert.True(close.IsCancel);
                dialog.Close();
                completion.SetResult();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
            finally
            {
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        await completion.Task.WaitAsync(TimeSpan.FromSeconds(15));
        thread.Join(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ViewModel_RestoresConfirmedSelectionAndRequestsDialogClose()
    {
        var current = CreateWorkspace();
        var restored = current with
        {
            WorkingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            HasUnsavedChanges = true,
        };
        var backups = new FakeBackupService(CreateBackupRecord())
        {
            RestoreResult = restored,
        };
        var viewModel = new BackupManagerDialogViewModel(
            backups,
            new FakeWorkspaceService(),
            current,
            _ => true);
        var closeRequested = false;
        viewModel.RequestClose += (_, _) => closeRequested = true;
        await viewModel.InitializeAsync(CancellationToken.None);

        viewModel.RestoreCommand.Execute(null);

        await WaitUntilAsync(
            () => viewModel.WasRestored && !viewModel.IsBusy,
            TimeSpan.FromSeconds(5));
        Assert.Same(restored, viewModel.Workspace);
        Assert.True(closeRequested);
        Assert.Equal(1, backups.RestoreCalls);
    }

    private static IEnumerable<DependencyObject> FindDescendants(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            yield return child;
            foreach (var descendant in FindDescendants(child))
            {
                yield return descendant;
            }
        }
    }
    private static ProjectWorkspace CreateWorkspace() => new(
        TimelineProject.Create(Guid.NewGuid(), "Sicherungstest", Timestamp),
        Path.GetTempPath(),
        Path.Combine(Path.GetTempPath(), "Sicherungstest.zeitprojekt"),
        false);

    private static BackupRecord CreateBackupRecord() => new(
        Guid.NewGuid(),
        Timestamp,
        "project/test.zeitprojekt",
        4_096,
        new string('a', 64),
        false);

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var stopAt = DateTime.UtcNow + timeout;
        while (!condition())
        {
            if (DateTime.UtcNow >= stopAt)
            {
                throw new TimeoutException("Die erwartete ViewModel-Aktualisierung blieb aus.");
            }

            await Task.Delay(10);
        }
    }

    private sealed class FakeBackupService(BackupRecord backup) : IBackupService
    {
        public int RetentionCalls { get; private set; }
        public int RestoreCalls { get; private set; }
        public ProjectWorkspace? RestoreResult { get; init; }

        public Task<BackupRecord> CreateAsync(
            ProjectWorkspace workspace,
            bool automatic,
            CancellationToken cancellationToken) => Task.FromResult(backup);

        public Task<BackupRecord?> CreateAutomaticIfDueAsync(
            ProjectWorkspace workspace,
            CancellationToken cancellationToken) => Task.FromResult<BackupRecord?>(null);

        public Task<IReadOnlyList<BackupRecord>> ListAsync(
            ProjectWorkspace workspace,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BackupRecord>>([backup]);

        public Task<ProjectWorkspace> RestoreAsync(
            ProjectWorkspace currentWorkspace,
            BackupRecord selectedBackup,
            CancellationToken cancellationToken)
        {
            RestoreCalls++;
            return Task.FromResult(RestoreResult ?? currentWorkspace);
        }

        public Task ApplyRetentionAsync(
            ProjectWorkspace workspace,
            CancellationToken cancellationToken)
        {
            RetentionCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset timestamp) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => timestamp;
    }

    private sealed class FakeWorkspaceService : IProjectWorkspaceService
    {
        public int CheckpointCalls { get; private set; }

        public Task<ProjectWorkspace> CheckpointAsync(
            ProjectWorkspace workspace,
            CancellationToken cancellationToken)
        {
            CheckpointCalls++;
            return Task.FromResult(workspace with { HasUnsavedChanges = true });
        }

        public Task<ProjectWorkspace> CreateAsync(
            string projectName,
            string archivePath,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ProjectWorkspace> OpenAsync(
            string archivePath,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ProjectWorkspace> SaveAsync(
            ProjectWorkspace workspace,
            string? targetArchivePath,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task ExportSnapshotAsync(
            ProjectWorkspace workspace,
            string targetArchivePath,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ProjectWorkspace> RestoreSnapshotAsync(
            ProjectWorkspace currentWorkspace,
            string snapshotArchivePath,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<ProjectWorkspace> DuplicateAsync(
            ProjectWorkspace workspace,
            string targetArchivePath,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task CloseAsync(
            ProjectWorkspace workspace,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task DeleteArchiveAsync(
            string archivePath,
            bool deletionConfirmed,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
