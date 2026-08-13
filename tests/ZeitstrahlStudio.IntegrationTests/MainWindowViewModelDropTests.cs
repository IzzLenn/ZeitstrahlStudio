using System.Reflection;
using ZeitstrahlStudio.App;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Shared;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class MainWindowViewModelDropTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2025, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ImportDroppedFilesAsync_UsesExplicitTargetForWholeBatchAndAuditsIt()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Drop-Test", Timestamp);
        var first = TimelineEvent.Create(
            Guid.NewGuid(),
            "Erstes Ereignis",
            EventDate.Year(2025),
            Timestamp);
        var second = TimelineEvent.Create(
            Guid.NewGuid(),
            "Zweites Ereignis",
            EventDate.Year(2026),
            Timestamp.AddSeconds(1));
        project.AddEvent(first, Timestamp.AddMinutes(1));
        project.AddEvent(second, Timestamp.AddMinutes(1));
        var workspace = new ProjectWorkspace(project, Path.GetTempPath(), "drop-test.zeitprojekt", false);
        var importedEventIds = new List<Guid>();
        var importedPaths = new List<string>();
        var auditEntries = new List<AuditEntry>();
        var checkpointCount = 0;

        var workspaceService = CreateProxy<IProjectWorkspaceService>(new Dictionary<string, Func<object?[], object?>>
        {
            [nameof(IProjectWorkspaceService.OpenAsync)] = _ => Task.FromResult(workspace),
            [nameof(IProjectWorkspaceService.CheckpointAsync)] = arguments =>
            {
                checkpointCount++;
                return Task.FromResult((ProjectWorkspace)arguments[0]!);
            },
        });
        var recentProjects = CreateProxy<IRecentProjectsService>(new Dictionary<string, Func<object?[], object?>>
        {
            [nameof(IRecentProjectsService.GetAsync)] = _ =>
                Task.FromResult<IReadOnlyList<RecentProject>>([]),
        });
        var recovery = CreateProxy<IProjectRecoveryService>(new Dictionary<string, Func<object?[], object?>>
        {
            [nameof(IProjectRecoveryService.FindAsync)] = _ =>
                Task.FromResult<IReadOnlyList<RecoveryCandidate>>([]),
        });
        var search = CreateProxy<IProjectSearchService>(new Dictionary<string, Func<object?[], object?>>
        {
            [nameof(IProjectSearchService.SearchAsync)] = _ =>
                Task.FromResult<IReadOnlyList<SearchResult>>([]),
        });
        var audit = CreateProxy<IAuditLogService>(new Dictionary<string, Func<object?[], object?>>
        {
            [nameof(IAuditLogService.WriteAsync)] = arguments =>
            {
                auditEntries.Add((AuditEntry)arguments[1]!);
                return Task.CompletedTask;
            },
        });
        var importer = CreateProxy<IAttachmentImportService>(new Dictionary<string, Func<object?[], object?>>
        {
            [nameof(IAttachmentImportService.ImportAsync)] = arguments =>
            {
                var eventId = (Guid)arguments[0]!;
                var paths = ((IReadOnlyCollection<string>)arguments[1]!).ToArray();
                importedEventIds.Add(eventId);
                importedPaths.AddRange(paths);
                var results = paths.Select(path => OperationResult<Attachment>.Success(
                    new Attachment(
                        Guid.NewGuid(),
                        Path.GetFileName(path),
                        "application/octet-stream",
                        1,
                        new string('a', 64),
                        path,
                        Timestamp,
                        $"attachments/{eventId:N}/{Guid.NewGuid():N}.bin")))
                    .ToArray();
                return Task.FromResult<IReadOnlyList<OperationResult<Attachment>>>(results);
            },
        });

        await using var viewModel = new MainWindowViewModel(
            workspaceService,
            recentProjects,
            recovery,
            CreateProxy<IProjectAutosaveService>(),
            CreateProxy<IBackupService>(),
            CreateProxy<ITimelineThumbnailService>(),
            CreateProxy<IApplicationThemeService>(),
            CreateProxy<ILocalLogService>(),
            audit,
            search,
            CreateProxy<IHtmlExportService>(),
            importer,
            CreateProxy<IAttachmentFileService>(),
            CreateProxy<IAttachmentAnalysisQueue>(),
            CreateProxy<IAttachmentAnalysisStore>(),
            new ProjectEventEditingService(),
            CreateProxy<IUserDialogService>());
        await viewModel.OpenPathAsync("drop-test.zeitprojekt");
        viewModel.SelectedEvent = first;
        var paths = new[] { @"C:\Quelle\eins.bin", @"C:\Quelle\zwei.bin" };

        await viewModel.ImportDroppedFilesAsync(paths, second.Id);

        Assert.Equal([second.Id], importedEventIds);
        Assert.Equal(paths, importedPaths);
        Assert.Empty(viewModel.CurrentProject!.Events.Single(item => item.Id == first.Id).Attachments);
        Assert.Equal(2, viewModel.CurrentProject.Events.Single(item => item.Id == second.Id).Attachments.Count);
        Assert.Equal(second.Id, viewModel.SelectedEvent?.Id);
        Assert.Equal(1, checkpointCount);
        var auditEntry = Assert.Single(auditEntries.Where(entry => entry.Operation == "AttachmentAdd"));
        Assert.Equal(second.Id, auditEntry.EntityId);
        Assert.Contains("2 Anhang", auditEntry.Description);
    }

    [Fact]
    public async Task OpenAttachmentDirectCommand_OpensOnlyClickedAttachmentOfSelectedEvent()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Öffnen-Test", Timestamp);
        var selectedEvent = TimelineEvent.Create(
            Guid.NewGuid(),
            "Ausgewähltes Ereignis",
            EventDate.Year(2025),
            Timestamp);
        var otherEvent = TimelineEvent.Create(
            Guid.NewGuid(),
            "Anderes Ereignis",
            EventDate.Year(2026),
            Timestamp.AddSeconds(1));
        var selectedAttachment = CreateAttachment("beleg.pdf");
        var otherAttachment = CreateAttachment("fremd.pdf");
        selectedEvent.AddAttachment(selectedAttachment, Timestamp.AddMinutes(1));
        otherEvent.AddAttachment(otherAttachment, Timestamp.AddMinutes(1));
        project.AddEvent(selectedEvent, Timestamp.AddMinutes(2));
        project.AddEvent(otherEvent, Timestamp.AddMinutes(2));
        var workspace = new ProjectWorkspace(project, Path.GetTempPath(), "öffnen-test.zeitprojekt", false);
        var opened = new TaskCompletionSource<(ProjectWorkspace Workspace, Attachment Attachment)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var attachmentFiles = CreateProxy<IAttachmentFileService>(
            new Dictionary<string, Func<object?[], object?>>
            {
                [nameof(IAttachmentFileService.OpenWithDefaultApplicationAsync)] = arguments =>
                {
                    opened.TrySetResult(((ProjectWorkspace)arguments[0]!, (Attachment)arguments[1]!));
                    return Task.CompletedTask;
                },
            });
        var dialogs = CreateProxy<IUserDialogService>(new Dictionary<string, Func<object?[], object?>>
        {
            [nameof(IUserDialogService.RequestAttachmentToOpen)] = _ =>
                throw new InvalidOperationException("Der direkte Doppelklick darf keinen Auswahldialog öffnen."),
        });

        await using var viewModel = CreateAttachmentOpenViewModel(workspace, attachmentFiles, dialogs);
        await viewModel.OpenPathAsync(workspace.ArchivePath!);
        viewModel.SelectedEvent = selectedEvent;

        Assert.True(viewModel.OpenAttachmentDirectCommand.CanExecute(selectedAttachment));
        Assert.False(viewModel.OpenAttachmentDirectCommand.CanExecute(otherAttachment));
        viewModel.OpenAttachmentDirectCommand.Execute(selectedAttachment);

        var actual = await opened.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await WaitUntilAsync(() => !viewModel.IsBusy);
        Assert.Same(workspace, actual.Workspace);
        Assert.Same(selectedAttachment, actual.Attachment);
        Assert.Contains("Windows-Standardprogramm", viewModel.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenAttachmentDirectCommand_BlocksRiskyFileButExplicitCommandStillOpensIt()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Sicherheits-Test", Timestamp);
        var timelineEvent = TimelineEvent.Create(
            Guid.NewGuid(),
            "Ereignis",
            EventDate.Year(2025),
            Timestamp);
        var riskyAttachment = CreateAttachment("wartung.cmd");
        timelineEvent.AddAttachment(riskyAttachment, Timestamp.AddMinutes(1));
        project.AddEvent(timelineEvent, Timestamp.AddMinutes(2));
        var workspace = new ProjectWorkspace(project, Path.GetTempPath(), "sicherheits-test.zeitprojekt", false);
        var opened = new TaskCompletionSource<Attachment>(TaskCreationOptions.RunContinuationsAsynchronously);
        var openCount = 0;
        var attachmentFiles = CreateProxy<IAttachmentFileService>(
            new Dictionary<string, Func<object?[], object?>>
            {
                [nameof(IAttachmentFileService.OpenWithDefaultApplicationAsync)] = arguments =>
                {
                    openCount++;
                    opened.TrySetResult((Attachment)arguments[1]!);
                    return Task.CompletedTask;
                },
            });
        var blocked = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var dialogs = CreateProxy<IUserDialogService>(new Dictionary<string, Func<object?[], object?>>
        {
            [nameof(IUserDialogService.RequestAttachmentToOpen)] = _ => riskyAttachment,
            [nameof(IUserDialogService.ShowError)] = arguments =>
            {
                blocked.TrySetResult((string)arguments[0]!);
                return null;
            },
        });

        await using var viewModel = CreateAttachmentOpenViewModel(workspace, attachmentFiles, dialogs);
        await viewModel.OpenPathAsync(workspace.ArchivePath!);
        viewModel.SelectedEvent = timelineEvent;

        Assert.True(viewModel.OpenAttachmentDirectCommand.CanExecute(riskyAttachment));
        viewModel.OpenAttachmentDirectCommand.Execute(riskyAttachment);

        var message = await blocked.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await WaitUntilAsync(() => !viewModel.IsBusy);
        Assert.Equal(0, openCount);
        Assert.Contains("nicht per Doppelklick geöffnet", message, StringComparison.Ordinal);
        Assert.Contains("Schaltfläche „Öffnen“", message, StringComparison.Ordinal);

        Assert.True(viewModel.OpenAttachmentCommand.CanExecute(null));
        viewModel.OpenAttachmentCommand.Execute(null);

        Assert.Same(riskyAttachment, await opened.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        await WaitUntilAsync(() => !viewModel.IsBusy);
        Assert.Equal(1, openCount);
    }

    private static MainWindowViewModel CreateAttachmentOpenViewModel(
        ProjectWorkspace workspace,
        IAttachmentFileService attachmentFiles,
        IUserDialogService dialogs)
    {
        var workspaceService = CreateProxy<IProjectWorkspaceService>(
            new Dictionary<string, Func<object?[], object?>>
            {
                [nameof(IProjectWorkspaceService.OpenAsync)] = _ => Task.FromResult(workspace),
            });
        var recentProjects = CreateProxy<IRecentProjectsService>(
            new Dictionary<string, Func<object?[], object?>>
            {
                [nameof(IRecentProjectsService.GetAsync)] = _ =>
                    Task.FromResult<IReadOnlyList<RecentProject>>([]),
            });
        var recovery = CreateProxy<IProjectRecoveryService>(
            new Dictionary<string, Func<object?[], object?>>
            {
                [nameof(IProjectRecoveryService.FindAsync)] = _ =>
                    Task.FromResult<IReadOnlyList<RecoveryCandidate>>([]),
            });
        var search = CreateProxy<IProjectSearchService>(
            new Dictionary<string, Func<object?[], object?>>
            {
                [nameof(IProjectSearchService.SearchAsync)] = _ =>
                    Task.FromResult<IReadOnlyList<SearchResult>>([]),
            });
        return new MainWindowViewModel(
            workspaceService,
            recentProjects,
            recovery,
            CreateProxy<IProjectAutosaveService>(),
            CreateProxy<IBackupService>(),
            CreateProxy<ITimelineThumbnailService>(),
            CreateProxy<IApplicationThemeService>(),
            CreateProxy<ILocalLogService>(),
            CreateProxy<IAuditLogService>(),
            search,
            CreateProxy<IHtmlExportService>(),
            CreateProxy<IAttachmentImportService>(),
            attachmentFiles,
            CreateProxy<IAttachmentAnalysisQueue>(),
            CreateProxy<IAttachmentAnalysisStore>(),
            new ProjectEventEditingService(),
            dialogs);
    }

    private static Attachment CreateAttachment(string fileName)
    {
        var id = Guid.NewGuid();
        return new Attachment(
            id,
            fileName,
            "application/octet-stream",
            1,
            new string('a', 64),
            null,
            Timestamp,
            $"attachments/{Guid.NewGuid():N}/{id:N}{Path.GetExtension(fileName)}",
            AttachmentState.Ready);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow.AddSeconds(5);
        while (!condition() && DateTime.UtcNow < timeout)
        {
            await Task.Delay(10);
        }

        Assert.True(condition());
    }

    private static T CreateProxy<T>(
        IReadOnlyDictionary<string, Func<object?[], object?>>? handlers = null)
        where T : class
    {
        var proxy = DispatchProxy.Create<T, TestDispatchProxy<T>>();
        ((TestDispatchProxy<T>)(object)proxy).Handlers = handlers ??
            new Dictionary<string, Func<object?[], object?>>();
        return proxy;
    }

    public class TestDispatchProxy<T> : DispatchProxy
        where T : class
    {
        public IReadOnlyDictionary<string, Func<object?[], object?>> Handlers { get; set; } =
            new Dictionary<string, Func<object?[], object?>>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is null)
            {
                throw new InvalidOperationException("Die aufgerufene Schnittstellenmethode fehlt.");
            }

            var arguments = args ?? [];
            if (Handlers.TryGetValue(targetMethod.Name, out var handler))
            {
                return handler(arguments);
            }

            var returnType = targetMethod.ReturnType;
            if (returnType == typeof(void))
            {
                return null;
            }

            if (returnType == typeof(Task))
            {
                return Task.CompletedTask;
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = returnType.GenericTypeArguments[0];
                var value = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
                var fromResult = typeof(Task).GetMethods()
                    .Single(method => method.Name == nameof(Task.FromResult) && method.IsGenericMethod)
                    .MakeGenericMethod(resultType);
                return fromResult.Invoke(null, [value]);
            }

            return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
        }
    }
}
