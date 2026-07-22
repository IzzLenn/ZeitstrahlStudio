using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Orchestriert die verbundene Projektverwaltung des Hauptfensters.</summary>
public sealed partial class MainWindowViewModel : ObservableObject, IAsyncDisposable
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");
    private readonly IProjectWorkspaceService workspaceService;
    private readonly IRecentProjectsService recentProjectsService;
    private readonly IProjectRecoveryService recoveryService;
    private readonly IProjectAutosaveService autosaveService;
    private readonly IBackupService backupService;
    private readonly ITimelineThumbnailService timelineThumbnailService;
    private readonly IApplicationThemeService themeService;
    private readonly ILocalLogService logService;
    private readonly IAuditLogService auditLogService;
    private readonly IProjectSearchService searchService;
    private readonly IHtmlExportService htmlExportService;
    private readonly IAttachmentImportService attachmentImportService;
    private readonly IAttachmentFileService attachmentFileService;
    private readonly IAttachmentAnalysisQueue attachmentAnalysisQueue;
    private readonly IAttachmentAnalysisStore attachmentAnalysisStore;
    private readonly ProjectEventEditingService eventEditingService;
    private readonly IUserDialogService dialogs;
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private CancellationTokenSource? attachmentImportCancellation;
    private ProjectWorkspace? currentWorkspace;
    private Task? autosaveTask;
    private SynchronizationContext? uiContext;
    private bool initialized;
    private bool isBusy;
    private bool isAttachmentImporting;
    private TimelineEvent? selectedEvent;
    private double timelineZoom = 1;
    private int timelineLayoutRevision;
    private DateTime? timelineRangeStart;
    private DateTime? timelineRangeEnd;
    private TimelineRangeRequest? timelineRangeRequest;
    private int timelineRangeRevision;
    private string statusMessage = "Bereit";

    public MainWindowViewModel(
        IProjectWorkspaceService workspaceService,
        IRecentProjectsService recentProjectsService,
        IProjectRecoveryService recoveryService,
        IProjectAutosaveService autosaveService,
        IBackupService backupService,
        ITimelineThumbnailService timelineThumbnailService,
        IApplicationThemeService themeService,
        ILocalLogService logService,
        IAuditLogService auditLogService,
        IProjectSearchService searchService,
        IHtmlExportService htmlExportService,
        IAttachmentImportService attachmentImportService,
        IAttachmentFileService attachmentFileService,
        IAttachmentAnalysisQueue attachmentAnalysisQueue,
        IAttachmentAnalysisStore attachmentAnalysisStore,
        ProjectEventEditingService eventEditingService,
        IUserDialogService dialogs)
    {
        this.workspaceService = workspaceService;
        this.recentProjectsService = recentProjectsService;
        this.recoveryService = recoveryService;
        this.autosaveService = autosaveService;
        this.backupService = backupService;
        this.timelineThumbnailService = timelineThumbnailService;
        this.themeService = themeService;
        this.logService = logService;
        this.auditLogService = auditLogService;
        this.searchService = searchService;
        this.htmlExportService = htmlExportService;
        this.attachmentImportService = attachmentImportService;
        this.attachmentFileService = attachmentFileService;
        this.attachmentAnalysisQueue = attachmentAnalysisQueue;
        this.attachmentAnalysisStore = attachmentAnalysisStore;
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
        UndoCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(UndoAsync),
            () => !IsBusy &&
                CurrentWorkspace is { } workspace &&
                eventEditingService.CanUndo(workspace.Project.Id));
        RedoCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(RedoAsync),
            () => !IsBusy &&
                CurrentWorkspace is { } workspace &&
                eventEditingService.CanRedo(workspace.Project.Id));
        MoveEventEarlierCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(() => MoveSelectedEventAsync(moveEarlier: true)),
            () => CanMoveSelectedEvent(moveEarlier: true));
        MoveEventLaterCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(() => MoveSelectedEventAsync(moveEarlier: false)),
            () => CanMoveSelectedEvent(moveEarlier: false));
        ReorderEventCommand = new AsyncRelayCommand<EventReorderRequest>(
            request => ExecuteGuardedAsync(() => ReorderDroppedEventAsync(request)),
            CanReorderEvent);
        SetHorizontalTimelineCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(() => SetTimelineOrientationAsync(TimelineOrientation.Horizontal)),
            () => !IsBusy && HasProject);
        SetVerticalTimelineCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(() => SetTimelineOrientationAsync(TimelineOrientation.Vertical)),
            () => !IsBusy && HasProject);
        ToggleGapCompressionCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(ToggleGapCompressionAsync),
            () => !IsBusy && HasProject);
        MoveTimelineCardCommand = new AsyncRelayCommand<TimelineCardMoveRequest>(
            request => ExecuteGuardedAsync(() => MoveTimelineCardAsync(request)),
            CanMoveTimelineCard);
        ResetTimelineLayoutCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(ResetTimelineLayoutAsync),
            () => !IsBusy && CurrentProject?.LayoutPositions.Count > 0);
        ShowTimelineRangeCommand = new AsyncRelayCommand(
            ShowTimelineRangeAsync,
            CanShowTimelineRange);
        ShowAuditLogCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(ShowAuditLogAsync),
            () => !IsBusy && HasProject);
        ManageBackupsCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(ManageBackupsAsync),
            () => !IsBusy && HasProject);
        SettingsCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(EditProjectSettingsAsync),
            () => !IsBusy && HasProject);
        AddAttachmentsCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(ChooseAndImportAttachmentsAsync),
            () => !IsBusy && SelectedEvent is not null);
        ImportDroppedFilesCommand = new AsyncRelayCommand<AttachmentDropRequest>(
            request => ExecuteGuardedAsync(() => ImportAttachmentPathsAsync(
                request.Paths,
                request.EventId)),
            CanImportDroppedFiles);
        AnalyzeAttachmentsCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(AnalyzeSelectedAttachmentsAsync),
            () => !IsBusy && SelectedEvent?.Attachments.Any(IsAnalyzableAttachment) == true);
        ShowAttachmentAnalysisCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(ShowAttachmentAnalysisAsync),
            () => !IsBusy && SelectedEvent?.Attachments.Count > 0);
        PreviewImageCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(PreviewImageAsync),
            () => !IsBusy && SelectedEvent?.Attachments.Any(IsImageAttachment) == true);
        PreviewPdfCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(PreviewPdfAsync),
            () => !IsBusy && SelectedEvent?.Attachments.Any(IsPdfAttachment) == true);
        PdfExportCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(ExportPdfAsync),
            () => !IsBusy && HasProject);
        HtmlExportCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(ExportHtmlAsync),
            () => !IsBusy && HasProject);
        OpenAttachmentCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(OpenAttachmentAsync),
            () => !IsBusy && SelectedEvent?.Attachments.Count > 0);
        RemoveAttachmentCommand = new AsyncRelayCommand(
            () => ExecuteGuardedAsync(RemoveAttachmentAsync),
            () => !IsBusy && SelectedEvent?.Attachments.Count > 0);
        CancelAttachmentImportCommand = new AsyncRelayCommand(
            CancelAttachmentImportAsync,
            () => IsAttachmentImporting);
        InitializeSearchPresentation();
        themeService.ThemeChanged += OnApplicationThemeChanged;
        themeService.Apply(ApplicationTheme.FollowWindows);
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
    public AsyncRelayCommand UndoCommand { get; }
    public AsyncRelayCommand RedoCommand { get; }
    public AsyncRelayCommand MoveEventEarlierCommand { get; }
    public AsyncRelayCommand MoveEventLaterCommand { get; }
    public AsyncRelayCommand<EventReorderRequest> ReorderEventCommand { get; }
    public AsyncRelayCommand SetHorizontalTimelineCommand { get; }
    public AsyncRelayCommand SetVerticalTimelineCommand { get; }
    public AsyncRelayCommand ToggleGapCompressionCommand { get; }
    public AsyncRelayCommand<TimelineCardMoveRequest> MoveTimelineCardCommand { get; }
    public AsyncRelayCommand ResetTimelineLayoutCommand { get; }
    public AsyncRelayCommand ShowTimelineRangeCommand { get; }
    public AsyncRelayCommand ShowAuditLogCommand { get; }
    public AsyncRelayCommand ManageBackupsCommand { get; }
    public AsyncRelayCommand SettingsCommand { get; }
    public AsyncRelayCommand AddAttachmentsCommand { get; }
    public AsyncRelayCommand<AttachmentDropRequest> ImportDroppedFilesCommand { get; }
    public AsyncRelayCommand AnalyzeAttachmentsCommand { get; }
    public AsyncRelayCommand ShowAttachmentAnalysisCommand { get; }
    public AsyncRelayCommand PreviewImageCommand { get; }
    public AsyncRelayCommand PreviewPdfCommand { get; }
    public AsyncRelayCommand PdfExportCommand { get; }
    public AsyncRelayCommand HtmlExportCommand { get; }
    public AsyncRelayCommand OpenAttachmentCommand { get; }
    public AsyncRelayCommand RemoveAttachmentCommand { get; }
    public AsyncRelayCommand CancelAttachmentImportCommand { get; }

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
    public ProjectWorkspace? ActiveWorkspace => CurrentWorkspace;
    public ITimelineThumbnailService TimelineThumbnailService => timelineThumbnailService;
    public string ProjectName => CurrentWorkspace?.Project.Name ?? "Kein Projekt geöffnet";
    public string ProjectDescription => CurrentWorkspace?.Project.Description ?? string.Empty;
    public string ProjectArchivePath => CurrentWorkspace?.ArchivePath ?? string.Empty;
    public TimelineProject? CurrentProject => CurrentWorkspace?.Project;
    public int EventCount => CurrentWorkspace?.Project.Events.Count ?? 0;
    public bool HasUnsavedChanges => CurrentWorkspace?.HasUnsavedChanges ?? false;
    public bool CanAcceptDroppedFiles => !IsBusy && HasProject && SelectedEvent is not null;
    public TimelineOrientation TimelineViewOrientation =>
        CurrentProject?.Settings.PreferredOrientation ?? TimelineOrientation.Horizontal;
    public bool CompressLargeGaps => CurrentProject?.Settings.CompressLargeGaps ?? true;
    public double TimelineCardFontSize => CurrentProject?.Settings.TimelineCardFontSize ?? 14;
    public double TimelineAxisFontSize => CurrentProject?.Settings.TimelineAxisFontSize ?? 12;
    public bool IsDarkTheme => themeService.IsDark;
    public string GapCompressionText => CompressLargeGaps
        ? "Zeitlücken: komprimiert"
        : "Zeitlücken: vollständig";
    public int TimelineLayoutRevision => timelineLayoutRevision;

    public double TimelineZoom
    {
        get => timelineZoom;
        set
        {
            var normalized = double.IsFinite(value) ? Math.Clamp(value, 0.25, 8) : 1;
            if (SetProperty(ref timelineZoom, normalized))
            {
                OnPropertyChanged(nameof(TimelineZoomText));
            }
        }
    }

    public string TimelineZoomText => $"{TimelineZoom:P0}";

    public DateTime? TimelineRangeStart
    {
        get => timelineRangeStart;
        set
        {
            if (SetProperty(ref timelineRangeStart, value))
            {
                OnTimelineRangeInputChanged();
            }
        }
    }

    public DateTime? TimelineRangeEnd
    {
        get => timelineRangeEnd;
        set
        {
            if (SetProperty(ref timelineRangeEnd, value))
            {
                OnTimelineRangeInputChanged();
            }
        }
    }

    public TimelineRangeRequest? TimelineRangeRequest
    {
        get => timelineRangeRequest;
        private set => SetProperty(ref timelineRangeRequest, value);
    }

    public string TimelineRangeText => TimelineRangeStart.HasValue && TimelineRangeEnd.HasValue
        ? $"Zeitraum: {TimelineRangeStart:dd.MM.yyyy} – {TimelineRangeEnd:dd.MM.yyyy}"
        : "Zeitraum: nicht gewählt";

    public bool IsAttachmentImporting
    {
        get => isAttachmentImporting;
        private set
        {
            if (SetProperty(ref isAttachmentImporting, value))
            {
                CancelAttachmentImportCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public TimelineEvent? SelectedEvent
    {
        get => selectedEvent;
        set
        {
            if (SetProperty(ref selectedEvent, value))
            {
                EditEventCommand.RaiseCanExecuteChanged();
                DeleteEventCommand.RaiseCanExecuteChanged();
                MoveEventEarlierCommand.RaiseCanExecuteChanged();
                MoveEventLaterCommand.RaiseCanExecuteChanged();
                AddAttachmentsCommand.RaiseCanExecuteChanged();
                AnalyzeAttachmentsCommand.RaiseCanExecuteChanged();
                ShowAttachmentAnalysisCommand.RaiseCanExecuteChanged();
                PreviewImageCommand.RaiseCanExecuteChanged();
                PreviewPdfCommand.RaiseCanExecuteChanged();
                OpenAttachmentCommand.RaiseCanExecuteChanged();
                RemoveAttachmentCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(CanAcceptDroppedFiles));
                OnPropertyChanged(nameof(HasSelectedEvent));
                OnPropertyChanged(nameof(HasNoSelectedEvent));
                OnPropertyChanged(nameof(SelectedEventDateText));
                OnPropertyChanged(nameof(SelectedEventDescriptionText));
                OnPropertyChanged(nameof(SelectedEventInfoText));
                OnPropertyChanged(nameof(SelectedEventPriorityText));
                OnPropertyChanged(nameof(SelectedEventStatusText));
                OnPropertyChanged(nameof(SelectedEventTagsText));
                OnPropertyChanged(nameof(SelectedEventDeadlineText));
                OnPropertyChanged(nameof(SelectedEventNotesText));
                OnPropertyChanged(nameof(SelectedEventAuditText));
            }
        }
    }

    public bool HasSelectedEvent => SelectedEvent is not null;

    public bool HasNoSelectedEvent => SelectedEvent is null;

    public string SelectedEventDateText => SelectedEvent?.Date.ToDisplayString(GermanCulture) ?? string.Empty;

    public string SelectedEventDescriptionText =>
        SelectedEvent?.Description ?? "Keine Beschreibung vorhanden.";

    public string SelectedEventInfoText =>
        SelectedEvent?.InfoText ?? "Keine Zusatzinformation vorhanden.";

    public string SelectedEventPriorityText => SelectedEvent?.Priority switch
    {
        EventPriority.Low => "Niedrig",
        EventPriority.Normal => "Normal",
        EventPriority.High => "Hoch",
        EventPriority.Critical => "Kritisch",
        _ => "–",
    };

    public string SelectedEventStatusText => SelectedEvent?.Status switch
    {
        EventStatus.Active => "Offen",
        EventStatus.Completed => "Abgeschlossen",
        EventStatus.Archived => "Archiviert",
        _ => "–",
    };

    public string SelectedEventTagsText => SelectedEvent is { Tags.Count: > 0 } timelineEvent
        ? string.Join(", ", timelineEvent.Tags.Order(StringComparer.CurrentCultureIgnoreCase))
        : "Keine Schlagwörter";

    public string SelectedEventDeadlineText => SelectedEvent?.Deadline is { } deadline
        ? FormatDeadline(deadline)
        : "Keine Frist";

    public string SelectedEventNotesText => SelectedEvent?.Notes ?? "Keine Notizen vorhanden.";

    public string SelectedEventAuditText => SelectedEvent is { } timelineEvent
        ? $"Erstellt {timelineEvent.CreatedAtUtc.ToLocalTime().ToString("g", GermanCulture)} · Geändert {timelineEvent.ModifiedAtUtc.ToLocalTime().ToString("g", GermanCulture)}"
        : string.Empty;

    private static string FormatDeadline(Deadline deadline)
    {
        var dueAt = deadline.DueDate.ToString("d", GermanCulture);
        if (deadline.DueTime is { } time)
        {
            dueAt += $" {time:HH\\:mm}";
        }

        var status = deadline.Status switch
        {
            DeadlineStatus.Open => "offen",
            DeadlineStatus.Completed => "abgeschlossen",
            DeadlineStatus.Cancelled => "entfallen",
            _ => string.Empty,
        };
        var label = deadline.Label is { Length: > 0 } ? $" · {deadline.Label}" : string.Empty;
        return $"{dueAt} · {status}{label}";
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
            InitializeTimelineRange();
            ResetSearchForWorkspace();
            themeService.Apply(value?.Project.Settings.Theme ?? ApplicationTheme.FollowWindows);

            OnPropertyChanged(nameof(HasProject));
            OnPropertyChanged(nameof(HasNoProject));
            OnPropertyChanged(nameof(ActiveWorkspace));
            OnPropertyChanged(nameof(ProjectName));
            OnPropertyChanged(nameof(ProjectDescription));
            OnPropertyChanged(nameof(ProjectArchivePath));
            OnPropertyChanged(nameof(CurrentProject));
            OnPropertyChanged(nameof(EventCount));
            OnPropertyChanged(nameof(HasUnsavedChanges));
            TimelineZoom = 1;
            RefreshTimelinePresentation();
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

    /// <summary>Importiert explizit per Drag-and-drop übergebene lokale Dateien.</summary>
    public Task ImportDroppedFilesAsync(IReadOnlyList<string> paths) =>
        SelectedEvent is { } timelineEvent
            ? ImportDroppedFilesAsync(paths, timelineEvent.Id)
            : Task.CompletedTask;

    /// <summary>Importiert abgelegte Dateien in das ausdrücklich bestimmte Ereignis.</summary>
    public Task ImportDroppedFilesAsync(IReadOnlyList<string> paths, Guid eventId) =>
        ExecuteGuardedAsync(() => ImportAttachmentPathsAsync(paths, eventId));

    /// <summary>Prüft, ob eine Ereigniszeile als interne Drag-Quelle verwendet werden darf.</summary>
    public bool CanStartEventDrag(Guid eventId) =>
        !IsBusy && CurrentProject?.Events.Any(timelineEvent => timelineEvent.Id == eventId) == true;

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
        attachmentImportCancellation?.Cancel();
        DisposeSearchPresentation();
        themeService.ThemeChanged -= OnApplicationThemeChanged;
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
        var automaticBackup = await TryCreateDueAutomaticBackupAsync(CurrentWorkspace).ConfigureAwait(true);
        StatusMessage = automaticBackup switch
        {
            { Failed: true } => "Projekt wurde erstellt; die automatische Sicherung ist fehlgeschlagen.",
            { Created: true } => "Projekt wurde erstellt, gespeichert und automatisch gesichert.",
            _ => "Projekt wurde erstellt und gespeichert.",
        };
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

    private async Task AddEventAsync()
    {
        if (CurrentWorkspace is null)
        {
            return;
        }

        var request = dialogs.RequestEvent(
            timelineEvent: null,
            CurrentWorkspace.Project.Settings.DefaultEventColorHex);
        if (request is null)
        {
            return;
        }

        var timestampUtc = DateTimeOffset.UtcNow;
        var created = eventEditingService.Create(
            CurrentWorkspace.Project,
            request,
            timestampUtc);
        MarkCurrentProjectChanged(created.Id);
        await WriteAuditAsync(
            "Create",
            created.Id,
            $"Ereignis „{created.Title}“ erstellt",
            timestampUtc).ConfigureAwait(true);
        StatusMessage = "Ereignis wurde erstellt.";
    }

    private async Task EditSelectedEventAsync()
    {
        if (CurrentWorkspace is null || SelectedEvent is null)
        {
            return;
        }

        var eventId = SelectedEvent.Id;
        var request = dialogs.RequestEvent(
            SelectedEvent,
            CurrentWorkspace.Project.Settings.DefaultEventColorHex);
        if (request is null)
        {
            return;
        }

        var timestampUtc = DateTimeOffset.UtcNow;
        eventEditingService.Update(
            CurrentWorkspace.Project,
            eventId,
            request,
            timestampUtc);
        MarkCurrentProjectChanged(eventId);
        await WriteAuditAsync(
            "Update",
            eventId,
            $"Ereignis „{SelectedEvent?.Title ?? request.Title}“ bearbeitet",
            timestampUtc).ConfigureAwait(true);
        await CheckpointCurrentWorkspaceAsync(eventId, lifetimeCancellation.Token).ConfigureAwait(true);
        StatusMessage = "Ereignis wurde aktualisiert.";
    }

    private async Task DeleteSelectedEventAsync()
    {
        if (CurrentWorkspace is null ||
            SelectedEvent is null ||
            !dialogs.ConfirmDeleteEvent(SelectedEvent.Title))
        {
            return;
        }

        var eventId = SelectedEvent.Id;
        var eventTitle = SelectedEvent.Title;
        var timestampUtc = DateTimeOffset.UtcNow;
        eventEditingService.Delete(
            CurrentWorkspace.Project,
            eventId,
            timestampUtc);
        MarkCurrentProjectChanged(selectedEventId: null);
        await WriteAuditAsync(
            "Delete",
            eventId,
            $"Ereignis „{eventTitle}“ gelöscht",
            timestampUtc).ConfigureAwait(true);
        StatusMessage = $"Ereignis „{eventTitle}“ wurde gelöscht.";
    }

    private async Task UndoAsync()
    {
        if (CurrentWorkspace is null)
        {
            return;
        }

        var timestampUtc = DateTimeOffset.UtcNow;
        var result = eventEditingService.Undo(CurrentWorkspace.Project, timestampUtc);
        MarkCurrentProjectChanged(result.SelectedEventId);
        await WriteAuditAsync(
            result.Operation,
            result.SelectedEventId,
            result.Description,
            timestampUtc).ConfigureAwait(true);
        await CheckpointCurrentWorkspaceAsync(
            result.SelectedEventId,
            lifetimeCancellation.Token).ConfigureAwait(true);
        StatusMessage = result.Description;
    }

    private async Task RedoAsync()
    {
        if (CurrentWorkspace is null)
        {
            return;
        }

        var timestampUtc = DateTimeOffset.UtcNow;
        var result = eventEditingService.Redo(CurrentWorkspace.Project, timestampUtc);
        MarkCurrentProjectChanged(result.SelectedEventId);
        await WriteAuditAsync(
            result.Operation,
            result.SelectedEventId,
            result.Description,
            timestampUtc).ConfigureAwait(true);
        await CheckpointCurrentWorkspaceAsync(
            result.SelectedEventId,
            lifetimeCancellation.Token).ConfigureAwait(true);
        StatusMessage = result.Description;
    }

    private async Task MoveSelectedEventAsync(bool moveEarlier)
    {
        if (CurrentWorkspace is null || SelectedEvent is null)
        {
            return;
        }

        var eventId = SelectedEvent.Id;
        var eventTitle = SelectedEvent.Title;
        var timestampUtc = DateTimeOffset.UtcNow;
        if (!eventEditingService.MoveWithinSameDate(
                CurrentWorkspace.Project,
                eventId,
                moveEarlier,
                timestampUtc))
        {
            return;
        }

        MarkCurrentProjectChanged(eventId);
        var description = moveEarlier
            ? $"Ereignis „{eventTitle}“ früher eingeordnet"
            : $"Ereignis „{eventTitle}“ später eingeordnet";
        await WriteAuditAsync("Reorder", eventId, description, timestampUtc).ConfigureAwait(true);
        StatusMessage = description;
    }

    private async Task ReorderDroppedEventAsync(EventReorderRequest request)
    {
        if (CurrentWorkspace is null)
        {
            return;
        }

        var source = CurrentWorkspace.Project.Events.SingleOrDefault(
            timelineEvent => timelineEvent.Id == request.DraggedEventId);
        var target = CurrentWorkspace.Project.Events.SingleOrDefault(
            timelineEvent => timelineEvent.Id == request.TargetEventId);
        if (source is null || target is null)
        {
            return;
        }

        var timestampUtc = DateTimeOffset.UtcNow;
        var insertAfter = request.Placement == EventDropPlacement.After;
        if (!eventEditingService.ReorderWithinSameDate(
                CurrentWorkspace.Project,
                source.Id,
                target.Id,
                insertAfter,
                timestampUtc))
        {
            return;
        }

        MarkCurrentProjectChanged(source.Id);
        var description = insertAfter
            ? $"Ereignis „{source.Title}“ nach „{target.Title}“ eingeordnet"
            : $"Ereignis „{source.Title}“ vor „{target.Title}“ eingeordnet";
        await WriteAuditAsync("Reorder", source.Id, description, timestampUtc).ConfigureAwait(true);
        StatusMessage = description;
    }

    private async Task SetTimelineOrientationAsync(TimelineOrientation orientation)
    {
        if (CurrentWorkspace is null || CurrentWorkspace.Project.Settings.PreferredOrientation == orientation)
        {
            return;
        }

        var timestampUtc = DateTimeOffset.UtcNow;
        CurrentWorkspace.Project.ChangeSettings(
            CurrentWorkspace.Project.Settings with { PreferredOrientation = orientation },
            timestampUtc);
        MarkCurrentProjectChanged(SelectedEvent?.Id);
        var description = orientation == TimelineOrientation.Horizontal
            ? "Horizontale Zeitstrahlansicht aktiviert"
            : "Vertikale Zeitstrahlansicht aktiviert";
        await WriteAuditAsync(
            "TimelineOrientation",
            entityId: null,
            description,
            timestampUtc).ConfigureAwait(true);
        StatusMessage = description;
    }

    private async Task ToggleGapCompressionAsync()
    {
        if (CurrentWorkspace is null)
        {
            return;
        }

        var timestampUtc = DateTimeOffset.UtcNow;
        var enabled = !CurrentWorkspace.Project.Settings.CompressLargeGaps;
        CurrentWorkspace.Project.ChangeSettings(
            CurrentWorkspace.Project.Settings with { CompressLargeGaps = enabled },
            timestampUtc);
        MarkCurrentProjectChanged(SelectedEvent?.Id);
        var description = enabled
            ? "Komprimierung großer Zeitlücken aktiviert"
            : "Komprimierung großer Zeitlücken deaktiviert";
        await WriteAuditAsync(
            "TimelineGapCompression",
            entityId: null,
            description,
            timestampUtc).ConfigureAwait(true);
        StatusMessage = description;
    }

    private async Task MoveTimelineCardAsync(TimelineCardMoveRequest request)
    {
        if (CurrentWorkspace is null)
        {
            return;
        }

        var timelineEvent = CurrentWorkspace.Project.Events
            .SingleOrDefault(item => item.Id == request.EventId);
        if (timelineEvent is null)
        {
            return;
        }

        var timestampUtc = DateTimeOffset.UtcNow;
        var moved = eventEditingService.MoveLayoutPosition(
            CurrentWorkspace.Project,
            request.EventId,
            request.Orientation,
            request.HorizontalDelta,
            request.VerticalDelta,
            timestampUtc);
        if (moved is null)
        {
            return;
        }

        MarkCurrentProjectChanged(request.EventId);
        var orientationText = request.Orientation == TimelineOrientation.Horizontal
            ? "horizontalen"
            : "vertikalen";
        var description = $"Karte „{timelineEvent.Title}“ in der {orientationText} Ansicht verschoben";
        await WriteAuditAsync(
            "LayoutMove",
            request.EventId,
            description,
            timestampUtc).ConfigureAwait(true);
        StatusMessage = description + ".";
    }

    private async Task ResetTimelineLayoutAsync()
    {
        if (CurrentWorkspace is null)
        {
            return;
        }

        var timestampUtc = DateTimeOffset.UtcNow;
        if (!eventEditingService.ResetLayoutPositions(
                CurrentWorkspace.Project,
                SelectedEvent?.Id,
                timestampUtc))
        {
            return;
        }

        MarkCurrentProjectChanged(SelectedEvent?.Id);
        const string description = "Automatische Zeitstrahlanordnung wiederhergestellt";
        await WriteAuditAsync(
            "LayoutReset",
            entityId: null,
            description,
            timestampUtc).ConfigureAwait(true);
        StatusMessage = description + ".";
    }

    private Task ShowTimelineRangeAsync()
    {
        if (!CanShowTimelineRange())
        {
            return Task.CompletedTask;
        }

        var start = DateOnly.FromDateTime(TimelineRangeStart!.Value);
        var end = DateOnly.FromDateTime(TimelineRangeEnd!.Value);
        timelineRangeRevision = unchecked(timelineRangeRevision + 1);
        TimelineRangeRequest = new TimelineRangeRequest(start, end, timelineRangeRevision);
        StatusMessage = $"Zeitraum {start:dd.MM.yyyy} bis {end:dd.MM.yyyy} wird angezeigt.";
        return Task.CompletedTask;
    }

    private async Task ShowAuditLogAsync()
    {
        if (CurrentWorkspace is null)
        {
            return;
        }

        var entries = await auditLogService.ReadAsync(
            CurrentWorkspace,
            lifetimeCancellation.Token).ConfigureAwait(true);
        dialogs.ShowAuditLog(entries);
        StatusMessage = $"{entries.Count} Audit-Einträge geladen.";
    }

    private async Task ManageBackupsAsync()
    {
        if (CurrentWorkspace is null)
        {
            return;
        }

        var previousWorkspace = CurrentWorkspace;
        var result = await dialogs.ShowBackupManagerAsync(
            previousWorkspace,
            lifetimeCancellation.Token).ConfigureAwait(true);
        if (result.WasRestored)
        {
            await workspaceService.CloseAsync(
                previousWorkspace,
                lifetimeCancellation.Token).ConfigureAwait(true);
            eventEditingService.Clear(previousWorkspace.Project.Id);
            CurrentWorkspace = result.Workspace;
            if (CurrentWorkspace.ArchivePath is not null)
            {
                await recentProjectsService.RecordOpenedAsync(
                    CurrentWorkspace,
                    lifetimeCancellation.Token).ConfigureAwait(true);
            }

            await RefreshStartDataAsync().ConfigureAwait(true);
            StatusMessage = "Die Sicherung wurde wiederhergestellt. Bitte speichern Sie das Projekt.";
            return;
        }

        CurrentWorkspace = result.Workspace;
        if (result.SettingsChanged)
        {
            OnPropertyChanged(nameof(HasUnsavedChanges));
            RaiseCommandStates();
            StatusMessage = "Die Sicherungseinstellungen wurden gespeichert.";
        }
    }

    private async Task EditProjectSettingsAsync()
    {
        if (CurrentWorkspace is null)
        {
            return;
        }

        var settings = dialogs.RequestProjectSettings(CurrentWorkspace.Project.Settings);
        if (settings is null || settings == CurrentWorkspace.Project.Settings)
        {
            return;
        }

        var timestampUtc = DateTimeOffset.UtcNow;
        var selectedEventId = SelectedEvent?.Id;
        CurrentWorkspace.Project.ChangeSettings(settings, timestampUtc);
        themeService.Apply(settings.Theme);
        MarkCurrentProjectChanged(selectedEventId);
        await WriteProjectSettingsAuditAsync(timestampUtc).ConfigureAwait(true);
        await CheckpointCurrentWorkspaceAsync(
            selectedEventId,
            lifetimeCancellation.Token).ConfigureAwait(true);
        StatusMessage = "Die Projekteinstellungen wurden übernommen.";
    }

    private async Task WriteProjectSettingsAuditAsync(DateTimeOffset timestampUtc)
    {
        if (CurrentWorkspace is null)
        {
            return;
        }

        try
        {
            await auditLogService.WriteAsync(
                CurrentWorkspace,
                new AuditEntry(
                    Guid.NewGuid(),
                    timestampUtc,
                    "SettingsUpdate",
                    nameof(ProjectSettings),
                    CurrentWorkspace.Project.Id,
                    "Projektbezogene Darstellungs- und Exporteinstellungen geändert",
                    Succeeded: true,
                    TechnicalDetails: null),
                lifetimeCancellation.Token).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            await TryWriteLogAsync(
                LocalLogLevel.Warning,
                "SettingsAuditWriteFailed",
                "Die Einstellungsänderung konnte nicht im Audit protokolliert werden.",
                exception.ToString()).ConfigureAwait(true);
        }
    }

    private void OnApplicationThemeChanged(object? sender, EventArgs e) => PostToUi(() =>
    {
        OnPropertyChanged(nameof(IsDarkTheme));
        RefreshTimelinePresentation();
    });

    private async Task ChooseAndImportAttachmentsAsync()
    {
        var paths = dialogs.RequestAttachmentPaths();
        if (paths.Count > 0 && SelectedEvent is { } timelineEvent)
        {
            await ImportAttachmentPathsAsync(paths, timelineEvent.Id).ConfigureAwait(true);
        }
    }

    private async Task ImportAttachmentPathsAsync(IReadOnlyList<string> paths, Guid eventId)
    {
        if (CurrentWorkspace is null || paths.Count == 0 ||
            CurrentWorkspace.Project.Events.All(timelineEvent => timelineEvent.Id != eventId))
        {
            return;
        }

        var workspace = CurrentWorkspace;
        using var operationCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(lifetimeCancellation.Token);
        attachmentImportCancellation = operationCancellation;
        IsAttachmentImporting = true;
        try
        {
            var progress = new Progress<FileOperationProgress>(report =>
            {
                StatusMessage =
                    $"Importiere {report.CurrentItem}: {report.CompletedItems}/{report.TotalItems}, " +
                    $"{report.SuccessfulItems} erfolgreich, {report.FailedItems} fehlgeschlagen";
            });
            var results = await attachmentImportService.ImportAsync(
                eventId,
                paths,
                workspace.WorkingDirectory,
                progress,
                operationCancellation.Token).ConfigureAwait(true);
            if (CurrentWorkspace?.Project.Id != workspace.Project.Id ||
                CurrentWorkspace.Project.Events.All(timelineEvent => timelineEvent.Id != eventId))
            {
                throw new InvalidOperationException(
                    "Das Zielereignis ist nach dem Dateikopieren nicht mehr geöffnet.");
            }

            var successfulAttachments = results
                .Where(result => result.IsSuccess && result.Value is not null)
                .Select(result => result.Value!)
                .ToArray();
            if (successfulAttachments.Length > 0)
            {
                var timestampUtc = DateTimeOffset.UtcNow;
                eventEditingService.AddAttachments(
                    CurrentWorkspace.Project,
                    eventId,
                    successfulAttachments,
                    timestampUtc);
                MarkCurrentProjectChanged(eventId);
                await WriteAuditAsync(
                    "AttachmentAdd",
                    eventId,
                    $"{successfulAttachments.Length} Anhang/Anhänge hinzugefügt",
                    timestampUtc).ConfigureAwait(true);
                await CheckpointCurrentWorkspaceAsync(
                    eventId,
                    operationCancellation.Token).ConfigureAwait(true);
            }

            var failures = results.Where(result => !result.IsSuccess).ToArray();
            var analysisSummary = successfulAttachments.Any(IsAnalyzableAttachment)
                ? await AnalyzeAttachmentsForEventAsync(
                    eventId,
                    successfulAttachments,
                    checkpointBeforeAnalysis: false,
                    operationCancellation.Token).ConfigureAwait(true)
                : (Successful: 0, Failed: 0);
            StatusMessage =
                $"{successfulAttachments.Length} Anhang/Anhänge importiert, {failures.Length} fehlgeschlagen; " +
                $"{analysisSummary.Successful} analysiert, {analysisSummary.Failed} Analysefehler.";
            if (failures.Length > 0)
            {
                var details = string.Join(
                    Environment.NewLine,
                    failures.Take(5).Select(result =>
                        $"{result.Error?.UserMessage} {result.Error?.TechnicalDetails}".Trim()));
                dialogs.ShowError(
                    $"{failures.Length} Datei(en) konnten nicht importiert werden. " +
                    "Erfolgreiche Projektkopien bleiben erhalten.",
                    details);
            }
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            StatusMessage = "Der Anhangsimport wurde abgebrochen.";
        }
        finally
        {
            if (ReferenceEquals(attachmentImportCancellation, operationCancellation))
            {
                attachmentImportCancellation = null;
            }

            IsAttachmentImporting = false;
        }
    }

    private async Task AnalyzeSelectedAttachmentsAsync()
    {
        if (CurrentWorkspace is null || SelectedEvent is null)
        {
            return;
        }

        var eventId = SelectedEvent.Id;
        var attachments = SelectedEvent.Attachments.Where(IsAnalyzableAttachment).ToArray();
        using var operationCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(lifetimeCancellation.Token);
        attachmentImportCancellation = operationCancellation;
        IsAttachmentImporting = true;
        try
        {
            var summary = await AnalyzeAttachmentsForEventAsync(
                eventId,
                attachments,
                checkpointBeforeAnalysis: true,
                operationCancellation.Token).ConfigureAwait(true);
            StatusMessage =
                $"{summary.Successful} Anhang/Anhänge analysiert, {summary.Failed} fehlgeschlagen.";
        }
        catch (OperationCanceledException) when (operationCancellation.IsCancellationRequested)
        {
            StatusMessage = "Die Dokumentanalyse wurde abgebrochen.";
        }
        finally
        {
            if (ReferenceEquals(attachmentImportCancellation, operationCancellation))
            {
                attachmentImportCancellation = null;
            }

            IsAttachmentImporting = false;
        }
    }

    private async Task<(int Successful, int Failed)> AnalyzeAttachmentsForEventAsync(
        Guid eventId,
        IReadOnlyCollection<Attachment> attachments,
        bool checkpointBeforeAnalysis,
        CancellationToken cancellationToken)
    {
        var supported = attachments.Where(IsAnalyzableAttachment).ToArray();
        if (supported.Length == 0 || CurrentWorkspace is null)
        {
            return (0, 0);
        }

        if (checkpointBeforeAnalysis)
        {
            await CheckpointCurrentWorkspaceAsync(eventId, cancellationToken).ConfigureAwait(true);
        }

        var workspace = CurrentWorkspace
            ?? throw new InvalidOperationException("Das Projekt wurde während der Analyse geschlossen.");
        var progress = new Progress<FileOperationProgress>(report =>
        {
            StatusMessage =
                $"Analysiere {report.CurrentItem}: {report.CompletedItems}/{report.TotalItems}, " +
                $"{report.SuccessfulItems} erfolgreich, {report.FailedItems} fehlgeschlagen";
        });
        var outcomes = await attachmentAnalysisQueue.AnalyzeAsync(
            workspace,
            supported,
            progress,
            cancellationToken).ConfigureAwait(true);
        var states = outcomes.ToDictionary(
            outcome => outcome.Attachment.Id,
            outcome => outcome.Result.Value?.ExtractionMethod is
                TextExtractionMethod.Ocr or TextExtractionMethod.EmbeddedTextAndOcr
                ? AttachmentState.Warning
                : outcome.Result.IsSuccess
                    ? AttachmentState.Ready
                    : AttachmentState.Failed);
        eventEditingService.UpdateAttachmentStates(
            workspace.Project,
            eventId,
            states,
            DateTimeOffset.UtcNow);
        MarkCurrentProjectChanged(eventId);
        await CheckpointCurrentWorkspaceAsync(eventId, cancellationToken).ConfigureAwait(true);

        var successful = outcomes.Count(outcome => outcome.Result.IsSuccess);
        var failed = outcomes.Count - successful;
        await WriteAuditAsync(
            "AttachmentAnalysis",
            eventId,
            $"{successful} Anhang/Anhänge analysiert, {failed} fehlgeschlagen",
            DateTimeOffset.UtcNow).ConfigureAwait(true);
        if (failed > 0)
        {
            var details = string.Join(
                Environment.NewLine,
                outcomes
                    .Where(outcome => !outcome.Result.IsSuccess)
                    .Take(5)
                    .Select(outcome =>
                        $"{outcome.Result.Error?.UserMessage} " +
                        $"{outcome.Result.Error?.TechnicalDetails}".Trim()));
            dialogs.ShowError(
                $"{failed} Dokument(e) konnten nicht lokal analysiert werden.",
                details);
        }

        return (successful, failed);
    }

    private async Task ShowAttachmentAnalysisAsync()
    {
        if (CurrentWorkspace is null || SelectedEvent is null)
        {
            return;
        }

        var attachment = dialogs.RequestAttachmentForAnalysis(SelectedEvent);
        if (attachment is null)
        {
            return;
        }

        var result = await attachmentAnalysisStore.LoadAsync(
            CurrentWorkspace,
            attachment,
            lifetimeCancellation.Token).ConfigureAwait(true);
        dialogs.ShowAttachmentAnalysis(attachment, result);
        StatusMessage = result is null
            ? $"Für „{attachment.OriginalFileName}“ liegt noch keine Analyse vor."
            : $"Analyse von „{attachment.OriginalFileName}“ wurde geladen.";
    }

    private async Task PreviewImageAsync()
    {
        if (CurrentWorkspace is null || SelectedEvent is null)
        {
            return;
        }

        var images = SelectedEvent.Attachments.Where(IsImageAttachment).ToArray();
        var attachment = dialogs.RequestImageForPreview(images);
        if (attachment is null)
        {
            return;
        }

        StatusMessage = $"Projektkopie von „{attachment.OriginalFileName}“ wird geprüft …";
        var path = await attachmentFileService.GetValidatedLocalPathAsync(
            CurrentWorkspace,
            attachment,
            lifetimeCancellation.Token).ConfigureAwait(true);
        dialogs.ShowImagePreview(attachment, path);
        StatusMessage = $"Bildvorschau von „{attachment.OriginalFileName}“ wurde geschlossen.";
    }

    private async Task PreviewPdfAsync()
    {
        if (CurrentWorkspace is null || SelectedEvent is null)
        {
            return;
        }

        var workspace = CurrentWorkspace;
        var pdfs = SelectedEvent.Attachments.Where(IsPdfAttachment).ToArray();
        var attachment = dialogs.RequestPdfForPreview(pdfs);
        if (attachment is null)
        {
            return;
        }

        StatusMessage = $"Projektkopie von „{attachment.OriginalFileName}“ wird geprüft …";
        var path = await attachmentFileService.GetValidatedLocalPathAsync(
            workspace,
            attachment,
            lifetimeCancellation.Token).ConfigureAwait(true);
        await dialogs.ShowPdfPreviewAsync(
            attachment,
            path,
            cancellationToken => attachmentFileService.OpenWithDefaultApplicationAsync(
                workspace,
                attachment,
                cancellationToken),
            lifetimeCancellation.Token).ConfigureAwait(true);
        StatusMessage = $"PDF-Vorschau von „{attachment.OriginalFileName}“ wurde geschlossen.";
    }

    private async Task ExportPdfAsync()
    {
        if (CurrentWorkspace is null)
        {
            return;
        }

        var targetPath = await dialogs.ShowPdfExportAsync(
            CurrentWorkspace,
            lifetimeCancellation.Token).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            StatusMessage = "PDF-Export wurde geschlossen.";
            return;
        }

        var timestampUtc = DateTimeOffset.UtcNow;
        await WriteAuditAsync(
            "PdfExport",
            CurrentWorkspace.Project.Id,
            $"Zeitstrahl als PDF „{Path.GetFileName(targetPath)}“ exportiert",
            timestampUtc).ConfigureAwait(true);
        StatusMessage = $"PDF „{Path.GetFileName(targetPath)}“ wurde erfolgreich exportiert.";
    }

    private async Task ExportHtmlAsync()
    {
        if (CurrentWorkspace is null)
        {
            return;
        }

        var request = dialogs.RequestHtmlExport(CurrentWorkspace.Project);
        if (request is null)
        {
            StatusMessage = "HTML-Export wurde abgebrochen.";
            return;
        }

        StatusMessage = "Offlinefähige HTML-Momentaufnahme wird erzeugt …";
        await htmlExportService.ExportAsync(
            CurrentWorkspace,
            request.Options,
            request.TargetPath,
            lifetimeCancellation.Token).ConfigureAwait(true);
        var timestampUtc = DateTimeOffset.UtcNow;
        await WriteAuditAsync(
            "HtmlExport",
            CurrentWorkspace.Project.Id,
            $"Zeitstrahl als HTML „{Path.GetFileName(request.TargetPath)}“ exportiert",
            timestampUtc).ConfigureAwait(true);
        StatusMessage = $"HTML „{Path.GetFileName(request.TargetPath)}“ wurde erfolgreich exportiert.";
    }

    private async Task OpenAttachmentAsync()
    {
        if (CurrentWorkspace is null || SelectedEvent is null)
        {
            return;
        }

        var attachment = dialogs.RequestAttachmentToOpen(SelectedEvent);
        if (attachment is null)
        {
            return;
        }

        StatusMessage = $"Projektkopie von „{attachment.OriginalFileName}“ wird geprüft …";
        await attachmentFileService.OpenWithDefaultApplicationAsync(
            CurrentWorkspace,
            attachment,
            lifetimeCancellation.Token).ConfigureAwait(true);
        StatusMessage = $"„{attachment.OriginalFileName}“ wurde an das Windows-Standardprogramm übergeben.";
    }

    private async Task RemoveAttachmentAsync()
    {
        if (CurrentWorkspace is null || SelectedEvent is null)
        {
            return;
        }

        var eventId = SelectedEvent.Id;
        var attachment = dialogs.RequestAttachmentToRemove(SelectedEvent);
        if (attachment is null)
        {
            return;
        }

        var timestampUtc = DateTimeOffset.UtcNow;
        eventEditingService.RemoveAttachment(
            CurrentWorkspace.Project,
            eventId,
            attachment.Id,
            timestampUtc);
        MarkCurrentProjectChanged(eventId);
        await WriteAuditAsync(
            "AttachmentRemove",
            eventId,
            $"Anhang „{attachment.OriginalFileName}“ entfernt",
            timestampUtc).ConfigureAwait(true);
        await CheckpointCurrentWorkspaceAsync(eventId, lifetimeCancellation.Token).ConfigureAwait(true);
        StatusMessage = $"Anhang „{attachment.OriginalFileName}“ wurde entfernt.";
    }

    private Task CancelAttachmentImportAsync()
    {
        attachmentImportCancellation?.Cancel();
        StatusMessage = "Der laufende Vorgang wird abgebrochen …";
        return Task.CompletedTask;
    }

    private async Task CheckpointCurrentWorkspaceAsync(
        Guid? selectedEventId,
        CancellationToken cancellationToken)
    {
        if (currentWorkspace is null)
        {
            return;
        }

        currentWorkspace = await workspaceService.CheckpointAsync(
            currentWorkspace,
            cancellationToken).ConfigureAwait(true);
        OnPropertyChanged(nameof(HasUnsavedChanges));
        RefreshEventList(selectedEventId);
        RaiseCommandStates();
    }

    private static bool IsAnalyzableAttachment(Attachment attachment) =>
        attachment.MediaType is
            "application/pdf" or
            "image/png" or
            "image/jpeg" or
            "image/tiff" or
            "image/bmp" or
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" or
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static bool IsImageAttachment(Attachment attachment) =>
        attachment.MediaType is
            "image/png" or
            "image/jpeg" or
            "image/tiff" or
            "image/bmp";

    private static bool IsPdfAttachment(Attachment attachment) =>
        attachment.MediaType == "application/pdf";

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
        var automaticBackup = await TryCreateDueAutomaticBackupAsync(CurrentWorkspace).ConfigureAwait(true);
        StatusMessage = automaticBackup switch
        {
            { Failed: true } => "Projekt wurde gespeichert; die automatische Sicherung ist fehlgeschlagen.",
            { Created: true } => "Projekt wurde gespeichert und automatisch gesichert.",
            _ => "Projekt wurde gespeichert.",
        };
    }

    private async Task<(bool Created, bool Failed)> TryCreateDueAutomaticBackupAsync(
        ProjectWorkspace workspace)
    {
        try
        {
            var created = await backupService.CreateAutomaticIfDueAsync(
                workspace,
                lifetimeCancellation.Token).ConfigureAwait(true);
            return (created is not null, false);
        }
        catch (OperationCanceledException) when (lifetimeCancellation.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidDataException or
            InvalidOperationException or Microsoft.Data.Sqlite.SqliteException)
        {
            await TryWriteLogAsync(
                LocalLogLevel.Warning,
                "BackupAutomaticFailed",
                "Das Projekt wurde gespeichert, die automatische Sicherung ist jedoch fehlgeschlagen.",
                exception.ToString()).ConfigureAwait(true);
            return (false, true);
        }
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
        eventEditingService.Clear(previousWorkspace.Project.Id);
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
        eventEditingService.Clear(workspaceToClose.Project.Id);
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
        RefreshTimelinePresentation();
        RefreshSearchPresentation();
    }

    private void RefreshTimelinePresentation()
    {
        timelineLayoutRevision = unchecked(timelineLayoutRevision + 1);
        OnPropertyChanged(nameof(TimelineLayoutRevision));
        OnPropertyChanged(nameof(CurrentProject));
        OnPropertyChanged(nameof(TimelineViewOrientation));
        OnPropertyChanged(nameof(CompressLargeGaps));
        OnPropertyChanged(nameof(GapCompressionText));
        OnPropertyChanged(nameof(TimelineCardFontSize));
        OnPropertyChanged(nameof(TimelineAxisFontSize));
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(TimelineRangeText));
        ResetTimelineLayoutCommand.RaiseCanExecuteChanged();
    }

    private void InitializeTimelineRange()
    {
        TimelineRangeRequest = null;
        if (CurrentProject is null)
        {
            TimelineRangeStart = null;
            TimelineRangeEnd = null;
            return;
        }

        var chronological = CurrentProject.GetChronologicalEvents();
        TimelineRangeStart = CurrentProject.OverallStart?.ToDateTime(TimeOnly.MinValue) ??
            chronological.FirstOrDefault()?.Date.SortStart;
        TimelineRangeEnd = CurrentProject.OverallEnd?.ToDateTime(TimeOnly.MinValue) ??
            (chronological.Count > 0 ? chronological.Max(GetTimelineEventEnd) : null);
    }

    private void OnTimelineRangeInputChanged()
    {
        OnPropertyChanged(nameof(TimelineRangeText));
        ShowTimelineRangeCommand.RaiseCanExecuteChanged();
    }

    private bool CanShowTimelineRange() =>
        !IsBusy &&
        HasProject &&
        TimelineRangeStart.HasValue &&
        TimelineRangeEnd.HasValue &&
        TimelineRangeEnd.Value.Date >= TimelineRangeStart.Value.Date;

    private bool CanMoveTimelineCard(TimelineCardMoveRequest request) =>
        !IsBusy &&
        CurrentProject?.Events.Any(timelineEvent => timelineEvent.Id == request.EventId) == true &&
        double.IsFinite(request.HorizontalDelta) &&
        double.IsFinite(request.VerticalDelta);

    private bool CanReorderEvent(EventReorderRequest request) =>
        !IsBusy &&
        CurrentWorkspace is { } workspace &&
        eventEditingService.CanReorderWithinSameDate(
            workspace.Project,
            request.DraggedEventId,
            request.TargetEventId,
            request.Placement == EventDropPlacement.After);

    private bool CanImportDroppedFiles(AttachmentDropRequest request) =>
        !IsBusy &&
        request.EventId != Guid.Empty &&
        request.Paths.Count > 0 &&
        request.Paths.All(path => !string.IsNullOrWhiteSpace(path)) &&
        CurrentProject?.Events.Any(timelineEvent => timelineEvent.Id == request.EventId) == true;

    private static DateTime GetTimelineEventEnd(TimelineEvent timelineEvent) =>
        timelineEvent.Date.EndYear.HasValue
            ? new DateTime(
                timelineEvent.Date.EndYear.Value,
                timelineEvent.Date.EndMonth!.Value,
                timelineEvent.Date.EndDay!.Value)
            : timelineEvent.Date.SortStart;

    private bool CanMoveSelectedEvent(bool moveEarlier) =>
        !IsBusy &&
        CurrentWorkspace is { } workspace &&
        SelectedEvent is { } timelineEvent &&
        eventEditingService.CanMoveWithinSameDate(
            workspace.Project,
            timelineEvent.Id,
            moveEarlier);

    private async Task WriteAuditAsync(
        string operation,
        Guid? entityId,
        string description,
        DateTimeOffset timestampUtc)
    {
        if (CurrentWorkspace is null)
        {
            return;
        }

        try
        {
            await auditLogService.WriteAsync(
                CurrentWorkspace,
                new AuditEntry(
                    Guid.NewGuid(),
                    timestampUtc,
                    operation,
                    nameof(TimelineEvent),
                    entityId,
                    description,
                    Succeeded: true,
                    TechnicalDetails: null),
                lifetimeCancellation.Token).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            await TryWriteLogAsync(
                LocalLogLevel.Warning,
                "AuditWriteFailed",
                "Der lokale Audit-Eintrag konnte nicht geschrieben werden.",
                exception.ToString()).ConfigureAwait(true);
        }
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
        UndoCommand.RaiseCanExecuteChanged();
        RedoCommand.RaiseCanExecuteChanged();
        MoveEventEarlierCommand.RaiseCanExecuteChanged();
        MoveEventLaterCommand.RaiseCanExecuteChanged();
        ReorderEventCommand.RaiseCanExecuteChanged();
        SetHorizontalTimelineCommand.RaiseCanExecuteChanged();
        SetVerticalTimelineCommand.RaiseCanExecuteChanged();
        ToggleGapCompressionCommand.RaiseCanExecuteChanged();
        MoveTimelineCardCommand.RaiseCanExecuteChanged();
        ResetTimelineLayoutCommand.RaiseCanExecuteChanged();
        ShowTimelineRangeCommand.RaiseCanExecuteChanged();
        ShowAuditLogCommand.RaiseCanExecuteChanged();
        ManageBackupsCommand.RaiseCanExecuteChanged();
        SettingsCommand.RaiseCanExecuteChanged();
        AddAttachmentsCommand.RaiseCanExecuteChanged();
        ImportDroppedFilesCommand.RaiseCanExecuteChanged();
        AnalyzeAttachmentsCommand.RaiseCanExecuteChanged();
        ShowAttachmentAnalysisCommand.RaiseCanExecuteChanged();
        PreviewImageCommand.RaiseCanExecuteChanged();
        PreviewPdfCommand.RaiseCanExecuteChanged();
        PdfExportCommand.RaiseCanExecuteChanged();
        HtmlExportCommand.RaiseCanExecuteChanged();
        OpenAttachmentCommand.RaiseCanExecuteChanged();
        RemoveAttachmentCommand.RaiseCanExecuteChanged();
        CancelAttachmentImportCommand.RaiseCanExecuteChanged();
        RaiseSearchCommandStates();
        OnPropertyChanged(nameof(CanAcceptDroppedFiles));
    }
}
