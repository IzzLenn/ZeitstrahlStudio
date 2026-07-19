using System.Collections.ObjectModel;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Für die Sicherungsverwaltung aufbereiteter unveränderlicher Listeneintrag.</summary>
public sealed record BackupDisplayItem(
    BackupRecord Record,
    DateTimeOffset CreatedAtLocal,
    string TypeDisplay,
    string FileSizeDisplay);

/// <summary>Orchestriert Sicherungsliste, Aufbewahrung und Wiederherstellung ohne WPF-Code-behind-Logik.</summary>
public sealed class BackupManagerDialogViewModel : ObservableObject
{
    private readonly IBackupService backupService;
    private readonly IProjectWorkspaceService workspaceService;
    private readonly Func<BackupDisplayItem, bool> confirmRestore;
    private readonly TimeProvider timeProvider;
    private readonly TimeZoneInfo localTimeZone;
    private CancellationToken cancellationToken;
    private ProjectWorkspace workspace;
    private BackupDisplayItem? selectedBackup;
    private int currentDayBackupCount;
    private int dailyBackupCount;
    private int weeklyBackupCount;
    private bool isBusy;
    private bool settingsChanged;
    private bool wasRestored;
    private string statusMessage = "Sicherungen werden geladen …";
    private string? errorMessage;

    public BackupManagerDialogViewModel(
        IBackupService backupService,
        IProjectWorkspaceService workspaceService,
        ProjectWorkspace workspace,
        Func<BackupDisplayItem, bool> confirmRestore,
        TimeProvider? timeProvider = null,
        TimeZoneInfo? localTimeZone = null)
    {
        this.backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
        this.workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.confirmRestore = confirmRestore ?? throw new ArgumentNullException(nameof(confirmRestore));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.localTimeZone = localTimeZone ?? TimeZoneInfo.Local;

        var settings = workspace.Project.Settings;
        currentDayBackupCount = settings.CurrentDayBackupCount;
        dailyBackupCount = settings.DailyBackupCount;
        weeklyBackupCount = settings.WeeklyBackupCount;

        RefreshCommand = new AsyncRelayCommand(
            () => RunAsync(LoadBackupsCoreAsync, "Die Sicherungsliste konnte nicht geladen werden."),
            () => !IsBusy);
        CreateManualBackupCommand = new AsyncRelayCommand(
            () => RunAsync(CreateManualBackupCoreAsync, "Die manuelle Sicherung konnte nicht erstellt werden."),
            () => !IsBusy);
        ApplySettingsCommand = new AsyncRelayCommand(
            () => RunAsync(ApplySettingsCoreAsync, "Die Aufbewahrungseinstellungen konnten nicht gespeichert werden."),
            () => !IsBusy);
        RestoreCommand = new AsyncRelayCommand(
            () => RunAsync(RestoreCoreAsync, "Die ausgewählte Sicherung konnte nicht wiederhergestellt werden."),
            () => !IsBusy && SelectedBackup is not null);
    }

    public ObservableCollection<BackupDisplayItem> Backups { get; } = [];

    public AsyncRelayCommand RefreshCommand { get; }

    public AsyncRelayCommand CreateManualBackupCommand { get; }

    public AsyncRelayCommand ApplySettingsCommand { get; }

    public AsyncRelayCommand RestoreCommand { get; }

    public event EventHandler? RequestClose;

    public ProjectWorkspace Workspace
    {
        get => workspace;
        private set => SetProperty(ref workspace, value);
    }

    public BackupDisplayItem? SelectedBackup
    {
        get => selectedBackup;
        set
        {
            if (SetProperty(ref selectedBackup, value))
            {
                RestoreCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int CurrentDayBackupCount
    {
        get => currentDayBackupCount;
        set => SetProperty(ref currentDayBackupCount, value);
    }

    public int DailyBackupCount
    {
        get => dailyBackupCount;
        set => SetProperty(ref dailyBackupCount, value);
    }

    public int WeeklyBackupCount
    {
        get => weeklyBackupCount;
        set => SetProperty(ref weeklyBackupCount, value);
    }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                RefreshCommand.RaiseCanExecuteChanged();
                CreateManualBackupCommand.RaiseCanExecuteChanged();
                ApplySettingsCommand.RaiseCanExecuteChanged();
                RestoreCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasBackups => Backups.Count > 0;

    public bool SettingsChanged
    {
        get => settingsChanged;
        private set => SetProperty(ref settingsChanged, value);
    }

    public bool WasRestored
    {
        get => wasRestored;
        private set => SetProperty(ref wasRestored, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public string? ErrorMessage
    {
        get => errorMessage;
        private set
        {
            if (SetProperty(ref errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        this.cancellationToken = cancellationToken;
        return RunAsync(LoadBackupsCoreAsync, "Die Sicherungsliste konnte nicht geladen werden.");
    }

    private async Task RunAsync(Func<Task> action, string errorMessage)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await action().ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusMessage = "Der Sicherungsvorgang wurde abgebrochen.";
        }
        catch (Exception exception)
        {
            ErrorMessage = $"{errorMessage} {exception.Message}";
            StatusMessage = errorMessage;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadBackupsCoreAsync()
    {
        StatusMessage = "Sicherungen werden geladen …";
        var records = await backupService.ListAsync(Workspace, cancellationToken).ConfigureAwait(true);
        var selectedId = SelectedBackup?.Record.Id;
        Backups.Clear();
        foreach (var record in records.OrderByDescending(item => item.CreatedAtUtc))
        {
            var localTimestamp = TimeZoneInfo.ConvertTime(record.CreatedAtUtc, localTimeZone);
            Backups.Add(new BackupDisplayItem(
                record,
                localTimestamp,
                record.IsAutomatic ? "Automatisch" : "Manuell",
                FormatFileSize(record.FileSize)));
        }

        SelectedBackup = selectedId.HasValue
            ? Backups.FirstOrDefault(item => item.Record.Id == selectedId.Value)
            : Backups.FirstOrDefault();
        OnPropertyChanged(nameof(HasBackups));
        StatusMessage = Backups.Count == 1
            ? "1 lokale Sicherung verfügbar."
            : $"{Backups.Count} lokale Sicherungen verfügbar.";
    }

    private async Task CreateManualBackupCoreAsync()
    {
        StatusMessage = "Manuelle Sicherung wird erstellt …";
        var created = await backupService.CreateAsync(
            Workspace,
            automatic: false,
            cancellationToken).ConfigureAwait(true);
        await LoadBackupsCoreAsync().ConfigureAwait(true);
        SelectedBackup = Backups.FirstOrDefault(item => item.Record.Id == created.Id);
        StatusMessage = "Die manuelle Sicherung wurde erfolgreich erstellt.";
    }

    private async Task ApplySettingsCoreAsync()
    {
        var updatedSettings = Workspace.Project.Settings with
        {
            CurrentDayBackupCount = CurrentDayBackupCount,
            DailyBackupCount = DailyBackupCount,
            WeeklyBackupCount = WeeklyBackupCount,
        };
        updatedSettings.Validate();

        if (updatedSettings != Workspace.Project.Settings)
        {
            Workspace.Project.ChangeSettings(updatedSettings, timeProvider.GetUtcNow());
            Workspace = Workspace with { HasUnsavedChanges = true };
            Workspace = await workspaceService.CheckpointAsync(
                Workspace,
                cancellationToken).ConfigureAwait(true);
            SettingsChanged = true;
        }

        StatusMessage = "Aufbewahrungsregeln werden angewendet …";
        await backupService.ApplyRetentionAsync(Workspace, cancellationToken).ConfigureAwait(true);
        await LoadBackupsCoreAsync().ConfigureAwait(true);
        StatusMessage = "Die Aufbewahrungseinstellungen wurden gespeichert.";
    }

    private async Task RestoreCoreAsync()
    {
        var selected = SelectedBackup;
        if (selected is null || !confirmRestore(selected))
        {
            return;
        }

        StatusMessage = "Sicherheitssicherung wird erstellt und Auswahl wiederhergestellt …";
        Workspace = await backupService.RestoreAsync(
            Workspace,
            selected.Record,
            cancellationToken).ConfigureAwait(true);
        WasRestored = true;
        StatusMessage = "Die Sicherung wurde wiederhergestellt. Das Projekt muss noch gespeichert werden.";
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024d:0.0} KB";
        }

        return $"{bytes / (1024d * 1024d):0.0} MB";
    }
}
