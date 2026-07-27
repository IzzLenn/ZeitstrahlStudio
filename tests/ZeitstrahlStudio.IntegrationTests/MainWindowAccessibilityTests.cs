using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ZeitstrahlStudio.App;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class MainWindowAccessibilityTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task MainWindow_ProvidesRequiredKeyboardGesturesAndAutomationNames()
    {
        await RunOnStaThread(() =>
        {
            var viewModel = CreateViewModelWithProject();
            try
            {
                var window = new MainWindow(viewModel);
                Layout(window, 1_280, 760);
                var bindings = window.InputBindings.OfType<KeyBinding>().ToArray();

                AssertGesture(bindings, Key.S, ModifierKeys.Control);
                AssertGesture(bindings, Key.F, ModifierKeys.Control);
                AssertGesture(bindings, Key.N, ModifierKeys.Control);
                AssertGesture(bindings, Key.Z, ModifierKeys.Control);
                AssertGesture(bindings, Key.Y, ModifierKeys.Control);
                AssertGesture(bindings, Key.Delete, ModifierKeys.None);
                AssertGesture(bindings, Key.Escape, ModifierKeys.None);

                Assert.Equal(
                    "Zeitstrahl Studio Hauptfenster",
                    AutomationProperties.GetName(window));
                var search = Assert.IsType<TextBox>(window.FindName("SearchQueryTextBox"));
                Assert.Equal("Volltextsuche", AutomationProperties.GetName(search));
                var eventList = Assert.IsType<ListBox>(window.FindName("EventList"));
                Assert.Equal(
                    "Chronologische Ereignisliste",
                    AutomationProperties.GetName(eventList));
                var timeline = Assert.IsType<TimelineView>(window.FindName("TimelineControl"));
                Assert.True(timeline.Focusable);
                Assert.Equal("Interaktiver Zeitstrahl", AutomationProperties.GetName(timeline));
                var peer = UIElementAutomationPeer.CreatePeerForElement(timeline);
                Assert.NotNull(peer);
                Assert.Equal("Interaktiver Zeitstrahl", peer.GetName());
                Assert.NotNull(FindAutomationNamedDescendant(
                    window,
                    "Anhangsbereich des ausgewählten Ereignisses"));
                Assert.NotNull(FindAutomationNamedDescendant(
                    window,
                    "Anhänge des ausgewählten Ereignisses"));

                Assert.Contains(
                    "Strg+S",
                    GetButtonToolTip(window, "Speichern"),
                    StringComparison.Ordinal);
                Assert.Contains(
                    "Strg+N",
                    GetButtonToolTip(window, "Hinzufügen"),
                    StringComparison.Ordinal);
                Assert.Contains(
                    "Entf",
                    GetButtonToolTip(window, "Löschen"),
                    StringComparison.Ordinal);
            }
            finally
            {
                DisposeViewModel(viewModel);
            }
        });
    }

    [Fact]
    public async Task MainWindow_ProvidesStructuredMenuAndCommandBarAtReferenceWidth()
    {
        await RunOnStaThread(() =>
        {
            var viewModel = CreateViewModelWithProject();
            try
            {
                var window = new MainWindow(viewModel);
                var content = Assert.IsAssignableFrom<FrameworkElement>(window.Content);
                content.Measure(new Size(1_280, 760));
                content.Arrange(new Rect(0, 0, 1_280, 760));
                content.UpdateLayout();

                var menu = Assert.IsType<Menu>(window.FindName("MainMenu"));
                Assert.Equal(
                    ["Datei", "Bearbeiten", "Ansicht", "Ereignis", "Werkzeuge", "Hilfe"],
                    menu.Items.OfType<MenuItem>()
                        .Select(item => (item.Header as string)?.Replace("_", string.Empty))
                        .ToArray());

                var commandBar = Assert.IsType<Border>(window.FindName("GlobalCommandBar"));
                var buttons = FindLogicalDescendants<Button>(commandBar).ToArray();
                var contents = buttons.Select(button => button.Content as string).ToArray();
                foreach (var required in new[]
                {
                    "Navigation",
                    "Neu",
                    "Öffnen",
                    "Speichern",
                    "Rückgängig",
                    "Wiederholen",
                    "Horizontal",
                    "Vertikal",
                    "Suchen",
                    "Analysieren",
                    "PDF",
                    "HTML",
                    "Einstellungen",
                })
                {
                    Assert.Contains(required, contents);
                }

                Assert.DoesNotContain(contents, content => content is "☰" or "＋" or "−" or "◀" or "▶");
                Assert.All(buttons, button =>
                {
                    Assert.True(button.ActualWidth > 0);
                    var rightEdge = button.TranslatePoint(new Point(button.ActualWidth, 0), commandBar).X;
                    Assert.InRange(rightEdge, 0, commandBar.ActualWidth + 0.1);
                });
                Assert.True(viewModel.SetHorizontalTimelineCommand.CanExecute(null));
                Assert.True(viewModel.SetVerticalTimelineCommand.CanExecute(null));
            }
            finally
            {
                DisposeViewModel(viewModel);
            }
        });
    }

    [Fact]
    public async Task MainWindow_TopCommandBarExposesSettingsBeforeAProjectIsOpened()
    {
        await RunOnStaThread(() =>
        {
            var viewModel = CreateViewModel(openProject: false);
            try
            {
                var window = new MainWindow(viewModel);
                Layout(window, 1_280, 760);

                Assert.True(viewModel.HasNoProject);
                Assert.True(viewModel.SettingsCommand.CanExecute(null));
                var commandBar = Assert.IsType<Border>(window.FindName("GlobalCommandBar"));
                var settings = Assert.Single(FindLogicalDescendants<Button>(commandBar)
                    .Where(button => AutomationProperties.GetName(button) == "Einstellungen"));
                Assert.Equal(Visibility.Visible, settings.Visibility);
                Assert.True(settings.IsEnabled);
                Assert.Null(FindAutomationNamedDescendant(window, "Einstellungen im Startbildschirm"));
            }
            finally
            {
                DisposeViewModel(viewModel);
            }
        });
    }

    [Fact]
    public async Task ProjectSettings_CanSwitchFromGlobalDarkToStoredProjectLightTheme()
    {
        ApplicationTheme? appliedTheme = null;
        var applicationTheme = CreateProxy<IApplicationThemeService>(
            new Dictionary<string, Func<object?[], object?>>
            {
                ["get_CurrentTheme"] = _ => ApplicationTheme.Dark,
                ["get_IsDark"] = _ => true,
                [nameof(IApplicationThemeService.ApplyAndSaveAsync)] = arguments =>
                {
                    appliedTheme = (ApplicationTheme)arguments[0]!;
                    return Task.CompletedTask;
                },
            });
        var dialogs = CreateProxy<IUserDialogService>(
            new Dictionary<string, Func<object?[], object?>>
            {
                [nameof(IUserDialogService.RequestProjectSettings)] = arguments =>
                {
                    var presented = Assert.IsType<ProjectSettings>(arguments[0]);
                    Assert.Equal(ApplicationTheme.Dark, presented.Theme);
                    return presented with { Theme = ApplicationTheme.Light };
                },
            });
        var viewModel = CreateViewModel(
            openProject: true,
            applicationThemeOverride: applicationTheme,
            dialogsOverride: dialogs,
            configureProject: project => project.ChangeSettings(
                project.Settings with { Theme = ApplicationTheme.Light },
                Timestamp.AddMinutes(1)));
        try
        {
            var method = typeof(MainWindowViewModel).GetMethod(
                "EditProjectSettingsAsync",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var task = Assert.IsAssignableFrom<Task>(method!.Invoke(viewModel, null));

            await task;

            Assert.Equal(ApplicationTheme.Light, appliedTheme);
            Assert.Equal(ApplicationTheme.Light, viewModel.CurrentProject!.Settings.Theme);
            Assert.Equal("Die Projekteinstellungen wurden übernommen.", viewModel.StatusMessage);
        }
        finally
        {
            await viewModel.DisposeAsync();
        }
    }
    [Theory]
    [InlineData(1.00)]
    [InlineData(1.25)]
    [InlineData(1.50)]
    [InlineData(2.00)]
    public async Task MainWindow_LaysOutProjectAtSupportedUiScales(double scale)
    {
        await RunOnStaThread(() =>
        {
            var viewModel = CreateViewModelWithProject();
            try
            {
                var window = new MainWindow(viewModel);
                const int logicalWidth = 960;
                const int logicalHeight = 600;
                var content = Assert.IsAssignableFrom<FrameworkElement>(window.Content);
                content.LayoutTransform = new ScaleTransform(scale, scale);
                var availableSize = new Size(logicalWidth * scale, logicalHeight * scale);
                content.Measure(availableSize);
                content.Arrange(new Rect(availableSize));
                content.UpdateLayout();

                Assert.True(double.IsFinite(content.DesiredSize.Width));
                Assert.True(double.IsFinite(content.DesiredSize.Height));
                Assert.InRange(content.DesiredSize.Width, 1, (logicalWidth * scale) + 0.1);
                Assert.InRange(content.DesiredSize.Height, 1, (logicalHeight * scale) + 0.1);
                var timeline = Assert.IsType<TimelineView>(window.FindName("TimelineControl"));
                Assert.True(timeline.ActualWidth > 0);
                Assert.True(timeline.ActualHeight > 0);
                Assert.True(double.IsFinite(timeline.ActualWidth));
                Assert.True(double.IsFinite(timeline.ActualHeight));
            }
            finally
            {
                DisposeViewModel(viewModel);
            }
        });
    }

    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1366, 768)]
    [InlineData(1600, 900)]
    [InlineData(1920, 1080)]
    [InlineData(2560, 1440)]
    public async Task MainWindow_KeepsThreePaneWorkspaceInsideSupportedWindowSizes(
        double width,
        double height)
    {
        await RunOnStaThread(() =>
        {
            var viewModel = CreateViewModelWithProject();
            try
            {
                var window = new MainWindow(viewModel);
                Layout(window, width, height);

                var content = Assert.IsAssignableFrom<FrameworkElement>(window.Content);
                var sidebar = Assert.IsType<Border>(window.FindName("SidebarPanel"));
                var inspector = Assert.IsType<Border>(window.FindName("InspectorPanel"));
                var timeline = Assert.IsType<TimelineView>(window.FindName("TimelineControl"));

                Assert.Equal(Visibility.Visible, sidebar.Visibility);
                Assert.Equal(Visibility.Visible, inspector.Visibility);
                Assert.InRange(sidebar.ActualWidth, 260, 380);
                Assert.InRange(inspector.ActualWidth, 280, 380);
                Assert.True(timeline.ActualWidth > 0);
                Assert.True(timeline.ActualHeight > 0);
                Assert.InRange(content.DesiredSize.Width, 1, width + 0.1);
                Assert.InRange(content.DesiredSize.Height, 1, height + 0.1);
                AssertInside(content, sidebar);
                AssertInside(content, timeline);
                AssertInside(content, inspector);
            }
            finally
            {
                DisposeViewModel(viewModel);
            }
        });
    }

    [Fact]
    public async Task MainWindow_GroupsTimelineToolsWithoutHorizontalOverflow()
    {
        await RunOnStaThread(() =>
        {
            var viewModel = CreateViewModelWithProject();
            try
            {
                var window = new MainWindow(viewModel);
                Layout(window, 1_280, 720);
                var tools = Assert.IsType<WrapPanel>(window.FindName("TimelineToolsPanel"));
                var buttons = FindLogicalDescendants<Button>(tools).ToArray();

                Assert.NotEmpty(buttons);
                Assert.DoesNotContain(
                    buttons.Select(button => button.Content as string),
                    content => content is "−" or "+");
                Assert.All(buttons, button =>
                {
                    Assert.True(button.ActualWidth > 0);
                    var rightEdge = button.TranslatePoint(new Point(button.ActualWidth, 0), tools).X;
                    Assert.InRange(rightEdge, 0, tools.ActualWidth + 0.1);
                });
            }
            finally
            {
                DisposeViewModel(viewModel);
            }
        });
    }

    [Fact]
    public async Task MainWindow_AllowsBothSidePanelsToBeCollapsedAndRestored()
    {
        await RunOnStaThread(() =>
        {
            var viewModel = CreateViewModelWithProject();
            try
            {
                var window = new MainWindow(viewModel);
                Layout(window, 1_280, 760);
                var sidebar = Assert.IsType<Border>(window.FindName("SidebarPanel"));
                var inspector = Assert.IsType<Border>(window.FindName("InspectorPanel"));
                var sidebarToggle = Assert.IsType<Button>(window.FindName("SidebarToggleButton"));
                var inspectorToggle = Assert.IsType<Button>(window.FindName("InspectorToggleButton"));

                Assert.Equal(Visibility.Visible, sidebar.Visibility);
                Assert.Equal(Visibility.Visible, inspector.Visibility);
                sidebarToggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                inspectorToggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(Visibility.Collapsed, sidebar.Visibility);
                Assert.Equal(Visibility.Collapsed, inspector.Visibility);

                sidebarToggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                inspectorToggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.Equal(Visibility.Visible, sidebar.Visibility);
                Assert.Equal(Visibility.Visible, inspector.Visibility);
            }
            finally
            {
                DisposeViewModel(viewModel);
            }
        });
    }

    private static void AssertInside(FrameworkElement container, FrameworkElement element)
    {
        var topLeft = element.TranslatePoint(new Point(0, 0), container);
        Assert.InRange(topLeft.X, -0.1, container.ActualWidth + 0.1);
        Assert.InRange(topLeft.Y, -0.1, container.ActualHeight + 0.1);
        Assert.InRange(topLeft.X + element.ActualWidth, 0, container.ActualWidth + 0.1);
        Assert.InRange(topLeft.Y + element.ActualHeight, 0, container.ActualHeight + 0.1);
    }
    private static MainWindowViewModel CreateViewModelWithProject() => CreateViewModel(openProject: true);

    private static MainWindowViewModel CreateViewModel(
        bool openProject,
        IApplicationThemeService? applicationThemeOverride = null,
        IUserDialogService? dialogsOverride = null,
        Action<TimelineProject>? configureProject = null)
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Zugänglichkeitstest", Timestamp);
        for (var index = 0; index < 4; index++)
        {
            project.AddEvent(
                TimelineEvent.Create(
                    Guid.NewGuid(),
                    $"Ereignis {index + 1}",
                    EventDate.Exact(new DateOnly(2020 + index, 7, 19)),
                    Timestamp),
                Timestamp);
        }

        configureProject?.Invoke(project);

        var workspace = new ProjectWorkspace(
            project,
            Path.GetTempPath(),
            Path.Combine(Path.GetTempPath(), "Zugänglichkeitstest.zeitprojekt"),
            HasUnsavedChanges: false);
        var workspaces = CreateProxy<IProjectWorkspaceService>(new Dictionary<string, Func<object?[], object?>>
        {
            [nameof(IProjectWorkspaceService.OpenAsync)] = _ => Task.FromResult(workspace),
            [nameof(IProjectWorkspaceService.CheckpointAsync)] = arguments =>
                Task.FromResult(Assert.IsType<ProjectWorkspace>(arguments[0])),
        });
        var recent = CreateProxy<IRecentProjectsService>(new Dictionary<string, Func<object?[], object?>>
        {
            [nameof(IRecentProjectsService.GetAsync)] = _ =>
                Task.FromResult<IReadOnlyList<RecentProject>>([]),
        });
        var recovery = CreateProxy<IProjectRecoveryService>(new Dictionary<string, Func<object?[], object?>>
        {
            [nameof(IProjectRecoveryService.FindAsync)] = _ =>
                Task.FromResult<IReadOnlyList<RecoveryCandidate>>([]),
        });
        var applicationTheme = applicationThemeOverride ?? CreateProxy<IApplicationThemeService>(
            new Dictionary<string, Func<object?[], object?>>
            {
                ["get_CurrentTheme"] = _ => ApplicationTheme.Dark,
                ["get_IsDark"] = _ => true,
                [nameof(IApplicationThemeService.Apply)] = _ => throw new InvalidOperationException(
                    "Ein Projektwechsel darf das globale Farbschema nicht anwenden oder überschreiben."),
            });
        var viewModel = new MainWindowViewModel(
            workspaces,
            recent,
            recovery,
            CreateProxy<IProjectAutosaveService>(),
            CreateProxy<IBackupService>(),
            CreateProxy<ITimelineThumbnailService>(),
            applicationTheme,
            CreateProxy<ILocalLogService>(),
            CreateProxy<IAuditLogService>(),
            CreateProxy<IProjectSearchService>(),
            CreateProxy<IHtmlExportService>(),
            CreateProxy<IAttachmentImportService>(),
            CreateProxy<IAttachmentFileService>(),
            CreateProxy<IAttachmentAnalysisQueue>(),
            CreateProxy<IAttachmentAnalysisStore>(),
            new ProjectEventEditingService(),
            dialogsOverride ?? CreateProxy<IUserDialogService>());
        if (openProject)
        {
            viewModel.OpenPathAsync(workspace.ArchivePath!).GetAwaiter().GetResult();
            Assert.True(viewModel.HasProject);
            Assert.True(viewModel.IsDarkTheme);
        }

        return viewModel;
    }

    private static void DisposeViewModel(MainWindowViewModel viewModel) =>
        viewModel.DisposeAsync().AsTask().GetAwaiter().GetResult();

    private static void Layout(MainWindow window, double width, double height)
    {
        var content = Assert.IsAssignableFrom<FrameworkElement>(window.Content);
        content.Measure(new Size(width, height));
        content.Arrange(new Rect(0, 0, width, height));
        content.UpdateLayout();
    }

    private static void AssertGesture(
        IEnumerable<KeyBinding> bindings,
        Key key,
        ModifierKeys modifiers) => Assert.Contains(
        bindings,
        binding => binding.Key == key && binding.Modifiers == modifiers);

    private static DependencyObject? FindAutomationNamedDescendant(
        DependencyObject root,
        string name)
    {
        if (AutomationProperties.GetName(root) == name)
        {
            return root;
        }

        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            var match = FindAutomationNamedDescendant(child, name);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static string GetButtonToolTip(DependencyObject root, string content)
    {
        var button = FindLogicalDescendants<Button>(root)
            .First(item => string.Equals(item.Content as string, content, StringComparison.Ordinal));
        return Assert.IsType<string>(button.ToolTip);
    }

    private static IEnumerable<T> FindLogicalDescendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in FindLogicalDescendants<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private static Task RunOnStaThread(Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                action();
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
        return AwaitCompletionAsync(completion.Task, thread);
    }

    private static async Task AwaitCompletionAsync(Task completion, Thread thread)
    {
        await completion.WaitAsync(TimeSpan.FromSeconds(30));
        thread.Join(TimeSpan.FromSeconds(5));
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

            if (Handlers.TryGetValue(targetMethod.Name, out var handler))
            {
                return handler(args ?? []);
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
                return typeof(Task)
                    .GetMethods()
                    .Single(method => method.Name == nameof(Task.FromResult) && method.IsGenericMethod)
                    .MakeGenericMethod(resultType)
                    .Invoke(null, [value]);
            }

            return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
        }
    }
}
