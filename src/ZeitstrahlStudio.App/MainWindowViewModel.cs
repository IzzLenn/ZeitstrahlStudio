using System.Collections.ObjectModel;
using System.IO;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Orchestriert die verbundene Projektverwaltung des Hauptfensters.</summary>
public sealed class MainWindowViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IProjectWorkspaceService workspaceService;
    private readonly IRecentProjectsService recentProjectsService;
    private readonly IProjectRecoveryService recoveryService;
    private readonly IProjectAutosaveService autosaveService;
    private readonly ILocalLogService logService;
    private readonly ProjectEventEditingService eventEditingService;
    private readonly IUserDialogService dialogs;
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private ProjectWorkspace? currentWorkspace;
    private Task? autosaveTask;
    private SynchronizationContext? uiContext;
    private bool initialized;
    private bool isBusy;
    private TimelineEvent? selectedEvent;
    private string statusMessage = "Bereit";

    public MainWindowViewModel(
        IProjectWorkspaceService workspaceService,
        IRecentProjectsService recentProjectsService,
        IProjectRecoveryService recoveryService,
        IProjectAutosaveService autosaveService,
        ILocalLogService logService,
        ProjectEventEditingService eventEditingService,
        IUserDialogService dialogs)
    {
        this.workspaceService = workspaceService;
        this.recentProjectsService = recentProjectsService;
        this.recoveryService = recoveryService;
        this.autosaveService = autosaveService;
        this.logService = logService;
        this.eventEditingService = eventEditingService;
        this.dialogs = dialogs;

        NewProjectCommand = new AsyncRelayCommand(() => ExecuteGuardedAsync(CreateProjectAsync), () => !IsBusy);
        OpenProjectCommand = new AsyncRelayCommand(() => ExecuteGuardedAsync(ChooseAndOpenProjectAsync), () => !IsBusy);
        OpenRecentCommand = new AsyncRelayCommand<RecentProject>(
            item => ExecuteGuardedAsync(() => OpenRecentAsync(item)),
            item => !IsBusy && item.FileExists);
        RecoverCommand = new AsyncRelayCommand<RecoveryCandidate>(
            item => ExecuteGuardedAsync(() => RecoverAsync(item)),
            _ => !IsBusy);
        DiscardRecoveryCommand = new AsyncRelayCommand<RecoveryCandidate>(
            item => ExecuteGuardedAsync(() => DiscardRecoveryAsync(item)),
            _ => !IsBusy);
        SaveCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(() => SaveCurrentAsync(targetPath: null)),
            () => !IsBusy && HasProject);
        SaveAsCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(SaveCurrentAsAsync),
            () => !IsBusy && HasProject);
        DuplicateCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(DuplicateCurrentAsync),
            () => !IsBusy && HasProject);
        CloseProjectCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(async () => _ = await CloseCurrentAsync().ConfigureAwait(true)),
            () => !IsBusy && HasProject);
        RefreshCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(RefreshStartDataAsync),
            () => !IsBusy);
        AddEventCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(AddEventAsync),
            () => !IsBusy && HasProject);
        EditEventCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(EditSelectedEventAsync),
            () => !IsBusy && SelectedEvent is not null);
        DeleteEventCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(DeleteSelectedEventAsync),
            () => !IsBusy && SelectedEvent is not null);
    }

    public ObservableCollection<RecentProject> RecentProjects { get; } = [];
    public ObservableCollection<RecoveryCandidate> RecoveryCandidates { get; } = [];
    public ObservableCollection<TimelineEvent> Events { get; } = [];

    public AsyncRelayCommand NewProjectCommand { get; }
    public AsyncRelayCommand OpenProjectCommand { get; }
    public AsyncRelayCommand<RecentProject> OpenRecentCommand { get; }
    public AsyncRelayCommand<RecoveryCandidate> RecoverCommand { get; }
    public AsyncRelayCommand<RecoveryCandidate> DiscardRecoveryCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand SaveAsCommand { get; }
    public AsyncRelayCommand DuplicateCommand { get; }
    public AsyncRelayCommand CloseProjectCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand AddEventCommand { get; }
    public AsyncRelayCommand EditEventCommand { get; }
    public AsyncRelayCommand DeleteEventCommand { get; }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public bool HasProject => CurrentWorkspace is not null;
    public bool HasNoProject => !HasProject;
    public string ProjectName => CurrentWorkspace?.Project.Name ?? "Kein Projekt geöffnet";
    public string ProjectDescription => CurrentWorkspace?.Project.Description ?? string.Empty;
    public string ProjectArchivePath => CurrentWorkspace?.ArchivePath ?? string.Empty;
    public int EventCount => CurrentWorkspace?.Project.Events.Count ?? 0;
    public bool HasUnsavedChanges => CurrentWorkspace?.HasUnsavedChanges ?? false;

    public TimelineEvent? SelectedEvent
    {
        get => selectedEvent;
        set
        {
            if (SetProperty(ref selectedEvent, value))
            {
                EditEventCommand.RaiseCanExecuteChanged();
                DeleteEventCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    private ProjectWorkspace? CurrentWorkspace
    {
        get => currentWorkspace;
        set
        {
            if (!SetProperty(ref currentWorkspace, value))
            {
                return;
            }

            RefreshEventList(selectedEventId: null);

            OnPropertyChanged(nameof(HasProject));
            OnPropertyChanged(nameof(HasNoProject));
            OnPropertyChanged(nameof(ProjectName));
            OnPropertyChanged(nameof(ProjectDescription));
            OnPropertyChanged(nameof(ProjectArchivePath));
            OnPropertyChanged(nameof(EventCount));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            RaiseCommandStates();
        }
    }

    /// <summary>Lädt Startdaten und startet die periodische Speicherung.</summary>
    public async Task InitializeAsync()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        uiContext = SynchronizationContext.Current;
        await ExecuteGuardedAsync(RefreshStartDataAsync).ConfigureAwait(true);
        var progress = new Progress<AutosaveStatus>(status =>
        {
            StatusMessage = status.Message;
            if (!status.Succeeded && status.Error is not null)
            {
                _ = TryWriteLogAsync(
                    LocalLogLevel.Warning,
                    "Autosave",
                    status.Error.UserMessage,
                    status.Error.TechnicalDetails);
            }
        });
        autosaveTask = autosaveService.RunAsync(
            () => IsBusy ? null : CurrentWorkspace,
            updated => PostToUi(() => CurrentWorkspace = updated),
            TimeSpan.FromSeconds(60),
            progress,
            lifetimeCancellation.Token);
    }

    /// <summary>Öffnet einen über die Kommandozeile übergebenen Projektpfad.</summary>
    public Task OpenPathAsync(string archivePath) =>
        ExecuteGuardedAsync(() => OpenProjectCoreAsync(archivePath));

    /// <summary>Bereitet einen geordneten Fensterschluss vor.</summary>
    public async Task<bool> PrepareToCloseAsync()
    {
        if (IsBusy)
        {
            return false;
        }

        IsBusy = true;
        try
        {
            return await CloseCurrentAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            await HandleErrorAsync("Das Projekt konnte nicht geschlossen werden.", exception).ConfigureAwait(true);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        lifetimeCancellation.Cancel();
        if (autosaveTask is not null)
        {
            try
            {
                await autosaveTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        lifetimeCancellation.Dispose();
    }

    private async Task CreateProjectAsync()
    {
        var projectName = dialogs.RequestProjectName();
        if (projectName is null)
        {
            return;
        }

        var archivePath = dialogs.RequestSaveProjectPath(projectName);
        if (archivePath is null || !await CloseCurrentAsync().ConfigureAwait(true))
        {
            return;
        }

        StatusMessage = "Projekt wird erstellt …";
        CurrentWorkspace = await workspaceService.CreateAsync(
            projectName,
            archivePath,
            lifetimeCancellation.Token).ConfigureAwait(true);
        await recentProjectsService.RecordOpenedAsync(CurrentWorkspace, lifetimeCancellation.Token)
            .ConfigureAwait(true);
        await RefreshStartDataAsync().ConfigureAwait(true);
        StatusMessage = "Projekt wurde erstellt und gespeichert.";
    }

    private async Task ChooseAndOpenProjectAsync()
    {
        var archivePath = dialogs.RequestOpenProjectPath();
        if (archivePath is not null)
        {
            await OpenProjectCoreAsync(archivePath).ConfigureAwait(true);
        }
    }

    private async Task OpenRecentAsync(RecentProject recentProject)
    {
        if (!recentProject.FileExists)
        {
            await recentProjectsService.RemoveAsync(recentProject.ArchivePath, lifetimeCancellation.Token)
                .ConfigureAwait(true);
            await RefreshStartDataAsync().ConfigureAwait(true);
            dialogs.ShowError("Das Projektarchiv wurde am gespeicherten Ort nicht gefunden.");
            return;
        }

        await OpenProjectCoreAsync(recentProject.ArchivePath).ConfigureAwait(true);
    }

    private async Task OpenProjectCoreAsync(string archivePath)
    {
        if (!await CloseCurrentAsync().ConfigureAwait(true))
        {
            return;
        }

        StatusMessage = "Projekt wird geprüft und geöffnet …";
        CurrentWorkspace = await workspaceService.OpenAsync(archivePath, lifetimeCancellation.Token)
            .ConfigureAwait(true);
        await recentProjectsService.RecordOpenedAsync(CurrentWorkspace, lifetimeCancellation.Token)
            .ConfigureAwait(true);
        await RefreshStartDataAsync().ConfigureAwait(true);
        StatusMessage = "Projekt ist geöffnet.";
    }

    private async Task RecoverAsync(RecoveryCandidate candidate)
    {
        if (!await CloseCurrentAsync().ConfigureAwait(true))
        {
            return;
        }

        CurrentWorkspace = await recoveryService.RecoverAsync(candidate, lifetimeCancellation.Token)
            .ConfigureAwait(true);
        if (CurrentWorkspace.ArchivePath is not null)
        {
            await recentProjectsService.RecordOpenedAsync(CurrentWorkspace, lifetimeCancellation.Token)
                .ConfigureAwait(true);
        }

        await RefreshStartDataAsync().ConfigureAwait(true);
        StatusMessage = "Die Arbeitskopie wurde wiederhergestellt. Bitte speichern Sie das Projekt.";
    }

    private async Task DiscardRecoveryAsync(RecoveryCandidate candidate)
    {
        if (!dialogs.ConfirmDiscardRecovery(candidate.ProjectName))
        {
            return;
        }

        await recoveryService.DiscardAsync(candidate, lifetimeCancellation.Token).ConfigureAwait(true);
        await RefreshStartDataAsync().ConfigureAwait(true);
        StatusMessage = "Die Arbeitskopie wurde verworfen.";
    }

    private Task AddEventAsync()
    {
        if (CurrentWorkspace is null)
        {
            return Task.CompletedTask;
        }

        var request = dialogs.RequestEvent(timelineEvent: null);
        if (request is null)
        {
            return Task.CompletedTask;
        }

        var created = eventEditingService.Create(
            CurrentWorkspace.Project,
            request,
            DateTimeOffset.UtcNow);
        MarkCurrentProjectChanged(created.Id);
        StatusMessage = "Ereignis wurde erstellt.";
        return Task.CompletedTask;
    }

    private Task EditSelectedEventAsync()
    {
        if (CurrentWorkspace is null || SelectedEvent is null)
        {
            return Task.CompletedTask;
        }

        var eventId = SelectedEvent.Id;
        var request = dialogs.RequestEvent(SelectedEvent);
        if (request is null)
        {
            return Task.CompletedTask;
        }

        eventEditingService.Update(
            CurrentWorkspace.Project,
            eventId,
            request,
            DateTimeOffset.UtcNow);
        MarkCurrentProjectChanged(eventId);
        StatusMessage = "Ereignis wurde aktualisiert.";
        return Task.CompletedTask;
    }

    private Task DeleteSelectedEventAsync()
    {
        if (CurrentWorkspace is null ||
            SelectedEvent is null ||
            !dialogs.ConfirmDeleteEvent(SelectedEvent.Title))
        {
            return Task.CompletedTask;
        }

        var eventTitle = SelectedEvent.Title;
        eventEditingService.Delete(
            CurrentWorkspace.Project,
            SelectedEvent.Id,
            DateTimeOffset.UtcNow);
        MarkCurrentProjectChanged(selectedEventId: null);
        StatusMessage = $"Ereignis „{eventTitle}“ wurde gelöscht.";
        return Task.CompletedTask;
    }

    private async Task SaveCurrentAsync(string? targetPath)
    {
        if (CurrentWorkspace is null)
        {
            return;
        }

        StatusMessage = "Projekt wird gespeichert …";
        CurrentWorkspace = await workspaceService.SaveAsync(
            CurrentWorkspace,
            targetPath,
            lifetimeCancellation.Token).ConfigureAwait(true);
        await recentProjectsService.RecordOpenedAsync(CurrentWorkspace, lifetimeCancellation.Token)
            .ConfigureAwait(true);
        await RefreshRecentProjectsAsync().ConfigureAwait(true);
        StatusMessage = "Projekt wurde gespeichert.";
    }

    private async Task SaveCurrentAsAsync()
    {
        if (CurrentWorkspace is null)
        {
            return;
        }

        var targetPath = dialogs.RequestSaveProjectPath(CurrentWorkspace.Project.Name);
        if (targetPath is not null)
        {
            await SaveCurrentAsync(targetPath).ConfigureAwait(true);
        }
    }

    private async Task DuplicateCurrentAsync()
    {
        if (CurrentWorkspace is null)
        {
            return;
        }

        var targetPath = dialogs.RequestSaveProjectPath(CurrentWorkspace.Project.Name + " – Kopie");
        if (targetPath is null)
        {
            return;
        }

        var previousWorkspace = CurrentWorkspace;
        StatusMessage = "Projektkopie wird erstellt …";
        var duplicate = await workspaceService.DuplicateAsync(
            previousWorkspace,
            targetPath,
            lifetimeCancellation.Token).ConfigureAwait(true);
        await workspaceService.CloseAsync(previousWorkspace, lifetimeCancellation.Token).ConfigureAwait(true);
        CurrentWorkspace = duplicate;
        await recentProjectsService.RecordOpenedAsync(duplicate, lifetimeCancellation.Token).ConfigureAwait(true);
        await RefreshStartDataAsync().ConfigureAwait(true);
        StatusMessage = "Projektkopie wurde erstellt und geöffnet.";
    }

    private async Task<bool> CloseCurrentAsync()
    {
        if (CurrentWorkspace is null)
        {
            return true;
        }

        if (CurrentWorkspace.HasUnsavedChanges)
        {
            var decision = dialogs.AskSaveChanges(CurrentWorkspace.Project.Name);
            if (decision == SaveChangesDecision.Cancel)
            {
                return false;
            }

            if (decision == SaveChangesDecision.Save)
            {
                await SaveCurrentAsync(targetPath: null).ConfigureAwait(true);
            }
        }

        var workspaceToClose = CurrentWorkspace;
        await workspaceService.CloseAsync(workspaceToClose, lifetimeCancellation.Token).ConfigureAwait(true);
        CurrentWorkspace = null;
        StatusMessage = "Projekt wurde geschlossen.";
        return true;
    }

    private async Task RefreshStartDataAsync()
    {
        await RefreshRecentProjectsAsync().ConfigureAwait(true);
        var recoveries = await recoveryService.FindAsync(lifetimeCancellation.Token).ConfigureAwait(true);
        RecoveryCandidates.Clear();
        foreach (var recovery in recoveries)
        {
            RecoveryCandidates.Add(recovery);
        }
    }

    private async Task RefreshRecentProjectsAsync()
    {
        var projects = await recentProjectsService.GetAsync(lifetimeCancellation.Token).ConfigureAwait(true);
        RecentProjects.Clear();
        foreach (var project in projects)
        {
            RecentProjects.Add(project);
        }
    }

    private void MarkCurrentProjectChanged(Guid? selectedEventId)
    {
        if (currentWorkspace is null)
        {
            return;
        }

        currentWorkspace = currentWorkspace with { HasUnsavedChanges = true };
        OnPropertyChanged(nameof(HasUnsavedChanges));
        RefreshEventList(selectedEventId);
        RaiseCommandStates();
    }

    private void RefreshEventList(Guid? selectedEventId)
    {
        Events.Clear();
        if (currentWorkspace is not null)
        {
            foreach (var timelineEvent in currentWorkspace.Project.GetChronologicalEvents())
            {
                Events.Add(timelineEvent);
            }
        }

        SelectedEvent = selectedEventId.HasValue
            ? Events.FirstOrDefault(timelineEvent => timelineEvent.Id == selectedEventId.Value)
            : null;
        OnPropertyChanged(nameof(EventCount));
    }

    private async Task ExecuteGuardedAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await action().ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (lifetimeCancellation.IsCancellationRequested)
        {
            StatusMessage = "Vorgang wurde abgebrochen.";
        }
        catch (Exception exception)
        {
            await HandleErrorAsync("Der Vorgang konnte nicht abgeschlossen werden.", exception).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task HandleErrorAsync(string userMessage, Exception exception)
    {
        StatusMessage = userMessage;
        await TryWriteLogAsync(LocalLogLevel.Error, "MainWindow", userMessage, exception.ToString())
            .ConfigureAwait(true);
        dialogs.ShowError(userMessage, exception.Message);
    }

    private async Task TryWriteLogAsync(
        LocalLogLevel level,
        string eventName,
        string message,
        string? technicalDetails)
    {
        try
        {
            await logService.WriteAsync(
                new LocalLogEntry(
                    DateTimeOffset.UtcNow,
                    level,
                    nameof(MainWindowViewModel),
                    eventName,
                    message,
                    technicalDetails),
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private void PostToUi(Action action)
    {
        if (uiContext is null)
        {
            action();
        }
        else
        {
            uiContext.Post(_ => action(), null);
        }
    }

    private void RaiseCommandStates()
    {
        NewProjectCommand.RaiseCanExecuteChanged();
        OpenProjectCommand.RaiseCanExecuteChanged();
        OpenRecentCommand.RaiseCanExecuteChanged();
        RecoverCommand.RaiseCanExecuteChanged();
        DiscardRecoveryCommand.RaiseCanExecuteChanged();
        SaveCommand.RaiseCanExecuteChanged();
        SaveAsCommand.RaiseCanExecuteChanged();
        DuplicateCommand.RaiseCanExecuteChanged();
        CloseProjectCommand.RaiseCanExecuteChanged();
        RefreshCommand.RaiseCanExecuteChanged();
        AddEventCommand.RaiseCanExecuteChanged();
        EditEventCommand.RaiseCanExecuteChanged();
        DeleteEventCommand.RaiseCanExecuteChanged();
    }
}
