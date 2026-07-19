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
