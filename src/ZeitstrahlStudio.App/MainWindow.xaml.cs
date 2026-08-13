using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Hauptfenster; Code-behind behandelt ausschließlich Fenster- und Darstellungsinteraktionen.</summary>
public partial class MainWindow : Window
{
    public static RoutedUICommand FocusSearchCommand { get; } = new(
        "Suche fokussieren",
        nameof(FocusSearchCommand),
        typeof(MainWindow));

    public static RoutedUICommand CreateContextCommand { get; } = new(
        "Projekt oder Ereignis erstellen",
        nameof(CreateContextCommand),
        typeof(MainWindow));

    public static RoutedUICommand DeleteSelectionCommand { get; } = new(
        "Ausgewähltes Ereignis löschen",
        nameof(DeleteSelectionCommand),
        typeof(MainWindow));

    private const string EventDragDataFormat = "ZeitstrahlStudio.EventId";
    private const double SidebarBreakpoint = 1040;
    private const double InspectorBreakpoint = 1180;
    private static readonly GridLength DefaultSidebarWidth = new(310);
    private static readonly GridLength DefaultInspectorWidth = new(300);
    private readonly MainWindowViewModel viewModel;
    private bool closeApproved;
    private bool closeInProgress;
    private Point eventDragStart;
    private TimelineEvent? eventDragSource;
    private ListBoxItem? eventDropIndicator;
    private bool? sidebarVisibilityOverride;
    private bool? inspectorVisibilityOverride;

    public MainWindow(MainWindowViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        CommandBindings.Add(new CommandBinding(
            FocusSearchCommand,
            ExecuteFocusSearch,
            CanFocusSearch));
        CommandBindings.Add(new CommandBinding(
            CreateContextCommand,
            ExecuteCreateContext,
            CanCreateContext));
        CommandBindings.Add(new CommandBinding(
            DeleteSelectionCommand,
            ExecuteDeleteSelection,
            CanDeleteSelection));
        UpdateResponsiveLayout(Width);
    }

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (closeApproved)
        {
            return;
        }

        e.Cancel = true;
        if (closeInProgress)
        {
            return;
        }

        closeInProgress = true;
        try
        {
            if (await viewModel.PrepareToCloseAsync().ConfigureAwait(true))
            {
                closeApproved = true;
                Close();
            }
        }
        finally
        {
            closeInProgress = false;
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (!TryGetDroppedPaths(e.Data, out var paths) || viewModel.SelectedEvent is not { } target)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var request = new AttachmentDropRequest(target.Id, paths);
        e.Effects = viewModel.ImportDroppedFilesCommand.CanExecute(request)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (!TryGetDroppedPaths(e.Data, out var paths) || viewModel.SelectedEvent is not { } target)
        {
            return;
        }

        var request = new AttachmentDropRequest(target.Id, paths);
        if (viewModel.ImportDroppedFilesCommand.CanExecute(request))
        {
            viewModel.ImportDroppedFilesCommand.Execute(request);
        }
    }

    private void EventList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        eventDragStart = e.GetPosition(EventList);
        eventDragSource = GetEventFromSource(EventList, e.OriginalSource);
    }

    private void EventList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (eventDragSource is null || e.LeftButton != MouseButtonState.Pressed)
        {
            eventDragSource = null;
            return;
        }

        var current = e.GetPosition(EventList);
        if (Math.Abs(current.X - eventDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - eventDragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var source = eventDragSource;
        eventDragSource = null;
        if (!viewModel.CanStartEventDrag(source.Id))
        {
            return;
        }

        viewModel.SelectedEvent = source;
        var data = new DataObject();
        data.SetData(EventDragDataFormat, source.Id.ToString("D"));
        try
        {
            _ = System.Windows.DragDrop.DoDragDrop(EventList, data, DragDropEffects.Move);
        }
        finally
        {
            ClearEventDropIndicator();
        }

        e.Handled = true;
    }

    private void EventList_DragOver(object sender, DragEventArgs e)
    {
        var targetItem = GetEventContainer(EventList, e.OriginalSource);
        var target = targetItem?.DataContext as TimelineEvent;
        if (TryGetDraggedEventId(e.Data, out var draggedEventId))
        {
            if (target is null)
            {
                ClearEventDropIndicator();
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            var placement = GetDropPlacement(e, targetItem!);
            var request = new EventReorderRequest(draggedEventId, target.Id, placement);
            var canExecute = viewModel.ReorderEventCommand.CanExecute(request);
            SetEventDropIndicator(
                targetItem,
                canExecute
                    ? placement == EventDropPlacement.Before ? "Before" : "After"
                    : null);
            e.Effects = canExecute ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
            return;
        }

        if (TryGetDroppedPaths(e.Data, out var paths))
        {
            target ??= viewModel.SelectedEvent;
            if (target is null)
            {
                ClearEventDropIndicator();
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            var request = new AttachmentDropRequest(target.Id, paths);
            var canExecute = viewModel.ImportDroppedFilesCommand.CanExecute(request);
            SetEventDropIndicator(targetItem, canExecute ? "DropFiles" : null);
            e.Effects = canExecute ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }
    }

    private void EventList_DragLeave(object sender, DragEventArgs e) => ClearEventDropIndicator();

    private void EventList_Drop(object sender, DragEventArgs e)
    {
        var targetItem = GetEventContainer(EventList, e.OriginalSource);
        var target = targetItem?.DataContext as TimelineEvent;
        try
        {
            if (TryGetDraggedEventId(e.Data, out var draggedEventId))
            {
                if (target is not null)
                {
                    var request = new EventReorderRequest(
                        draggedEventId,
                        target.Id,
                        GetDropPlacement(e, targetItem!));
                    if (viewModel.ReorderEventCommand.CanExecute(request))
                    {
                        viewModel.ReorderEventCommand.Execute(request);
                        e.Effects = DragDropEffects.Move;
                    }
                }

                e.Handled = true;
                return;
            }

            if (TryGetDroppedPaths(e.Data, out var paths))
            {
                target ??= viewModel.SelectedEvent;
                if (target is not null)
                {
                    var request = new AttachmentDropRequest(target.Id, paths);
                    if (viewModel.ImportDroppedFilesCommand.CanExecute(request))
                    {
                        viewModel.ImportDroppedFilesCommand.Execute(request);
                        e.Effects = DragDropEffects.Copy;
                    }
                }

                e.Handled = true;
            }
        }
        finally
        {
            ClearEventDropIndicator();
        }
    }

    private void AttachmentList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left ||
            sender is not ListBox listBox ||
            e.OriginalSource is not DependencyObject source ||
            ItemsControl.ContainerFromElement(listBox, source) is not ListBoxItem
            {
                DataContext: Attachment attachment,
            } ||
            !viewModel.OpenAttachmentDirectCommand.CanExecute(attachment))
        {
            return;
        }

        viewModel.OpenAttachmentDirectCommand.Execute(attachment);
        e.Handled = true;
    }

    private void AttachmentDropZone_DragOver(object sender, DragEventArgs e)
    {
        if (!TryGetDroppedPaths(e.Data, out var paths) || viewModel.SelectedEvent is not { } target)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var request = new AttachmentDropRequest(target.Id, paths);
        e.Effects = viewModel.ImportDroppedFilesCommand.CanExecute(request)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void AttachmentDropZone_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (!TryGetDroppedPaths(e.Data, out var paths) || viewModel.SelectedEvent is not { } target)
        {
            return;
        }

        var request = new AttachmentDropRequest(target.Id, paths);
        if (viewModel.ImportDroppedFilesCommand.CanExecute(request))
        {
            viewModel.ImportDroppedFilesCommand.Execute(request);
            e.Effects = DragDropEffects.Copy;
        }
    }

    private void TimelineZoomOut_Click(object sender, RoutedEventArgs e) => TimelineControl.ZoomOut();

    private void TimelineZoomIn_Click(object sender, RoutedEventArgs e) => TimelineControl.ZoomIn();

    private void TimelineShowAll_Click(object sender, RoutedEventArgs e) => TimelineControl.ShowWholeProject();

    private void TimelineCenterSelection_Click(object sender, RoutedEventArgs e) =>
        TimelineControl.CenterSelectedEvent();

    private void TimelineResetView_Click(object sender, RoutedEventArgs e) => TimelineControl.ResetView();

    private void About_Click(object sender, RoutedEventArgs e) =>
        MessageBox.Show(
            this,
            "Zeitstrahl Studio\nLokale Projektchroniken für Windows.",
            "Info zu Zeitstrahl Studio",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

    private void SearchFocus_Click(object sender, RoutedEventArgs e) => FocusSearch();

    private void CanFocusSearch(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = viewModel.HasProject;
        e.Handled = true;
    }

    private void ExecuteFocusSearch(object sender, ExecutedRoutedEventArgs e) => FocusSearch();

    private void CanCreateContext(object sender, CanExecuteRoutedEventArgs e)
    {
        var command = viewModel.HasProject
            ? viewModel.AddEventCommand
            : viewModel.NewProjectCommand;
        e.CanExecute = command.CanExecute(null);
        e.Handled = true;
    }

    private void ExecuteCreateContext(object sender, ExecutedRoutedEventArgs e)
    {
        var command = viewModel.HasProject
            ? viewModel.AddEventCommand
            : viewModel.NewProjectCommand;
        if (command.CanExecute(null))
        {
            command.Execute(null);
        }
    }

    private void CanDeleteSelection(object sender, CanExecuteRoutedEventArgs e)
    {
        var focused = Keyboard.FocusedElement;
        e.CanExecute = !IsTextEditingElement(focused) &&
            (IsDescendantOf(focused, EventList) || IsDescendantOf(focused, TimelineControl)) &&
            viewModel.DeleteEventCommand.CanExecute(null);
        e.Handled = true;
    }

    private void ExecuteDeleteSelection(object sender, ExecutedRoutedEventArgs e)
    {
        if (viewModel.DeleteEventCommand.CanExecute(null))
        {
            viewModel.DeleteEventCommand.Execute(null);
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) => UpdateResponsiveLayout(ActualWidth);

    private void Window_Closed(object? sender, EventArgs e) =>
        viewModel.PropertyChanged -= ViewModel_PropertyChanged;

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e) =>
        UpdateResponsiveLayout(e.NewSize.Width);

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.HasProject))
        {
            inspectorVisibilityOverride = null;
            UpdateResponsiveLayout(ActualWidth);
        }
    }

    private void FocusSearch()
    {
        sidebarVisibilityOverride = true;
        UpdateResponsiveLayout(ActualWidth);
        SearchExpander.IsExpanded = true;
        SearchQueryTextBox.Focus();
        SearchQueryTextBox.SelectAll();
    }

    private void SidebarToggle_Click(object sender, RoutedEventArgs e)
    {
        sidebarVisibilityOverride = SidebarPanel.Visibility != Visibility.Visible;
        UpdateResponsiveLayout(ActualWidth);
    }

    private void InspectorToggle_Click(object sender, RoutedEventArgs e)
    {
        inspectorVisibilityOverride = InspectorPanel.Visibility != Visibility.Visible;
        UpdateResponsiveLayout(ActualWidth);
    }

    private void UpdateResponsiveLayout(double availableWidth)
    {
        var width = availableWidth > 0 ? availableWidth : Width;
        var showSidebar = sidebarVisibilityOverride ?? width >= SidebarBreakpoint;
        var showInspector = viewModel.HasProject && (inspectorVisibilityOverride ?? width >= InspectorBreakpoint);

        SetPanelVisibility(SidebarPanel, SidebarSplitter, SidebarColumn, showSidebar, DefaultSidebarWidth);
        SetPanelVisibility(InspectorPanel, InspectorSplitter, InspectorColumn, showInspector, DefaultInspectorWidth);
    }

    private static void SetPanelVisibility(
        FrameworkElement panel,
        FrameworkElement splitter,
        ColumnDefinition column,
        bool visible,
        GridLength visibleWidth)
    {
        panel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        splitter.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        column.Width = visible ? visibleWidth : new GridLength(0);
    }

    private static bool IsTextEditingElement(IInputElement? inputElement)
    {
        var current = inputElement as DependencyObject;
        while (current is not null)
        {
            if (current is TextBoxBase or PasswordBox ||
                current is ComboBox { IsEditable: true })
            {
                return true;
            }

            current = GetParent(current);
        }

        return false;
    }

    private static bool IsDescendantOf(IInputElement? inputElement, DependencyObject ancestor)
    {
        var current = inputElement as DependencyObject;
        while (current is not null)
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }

            current = GetParent(current);
        }

        return false;
    }

    private static DependencyObject? GetParent(DependencyObject current) =>
        current is Visual or Visual3D
            ? VisualTreeHelper.GetParent(current)
            : LogicalTreeHelper.GetParent(current);

    private static bool TryGetDroppedPaths(IDataObject data, out IReadOnlyList<string> paths)
    {
        if (data.GetData(DataFormats.FileDrop) is string[] droppedPaths)
        {
            var normalized = droppedPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (normalized.Length > 0)
            {
                paths = normalized;
                return true;
            }
        }

        paths = [];
        return false;
    }

    private static bool TryGetDraggedEventId(IDataObject data, out Guid eventId)
    {
        eventId = Guid.Empty;
        return data.GetData(EventDragDataFormat) is string value && Guid.TryParse(value, out eventId);
    }

    private static TimelineEvent? GetEventFromSource(ListBox listBox, object source) =>
        GetEventContainer(listBox, source)?.DataContext as TimelineEvent;

    private static ListBoxItem? GetEventContainer(ListBox listBox, object source) =>
        source is DependencyObject dependencyObject
            ? ItemsControl.ContainerFromElement(listBox, dependencyObject) as ListBoxItem
            : null;

    private static EventDropPlacement GetDropPlacement(DragEventArgs e, ListBoxItem targetItem) =>
        e.GetPosition(targetItem).Y < targetItem.ActualHeight / 2
            ? EventDropPlacement.Before
            : EventDropPlacement.After;

    private void SetEventDropIndicator(ListBoxItem? targetItem, object? indicator)
    {
        if (!ReferenceEquals(eventDropIndicator, targetItem))
        {
            ClearEventDropIndicator();
            eventDropIndicator = targetItem;
        }

        if (eventDropIndicator is not null)
        {
            eventDropIndicator.Tag = indicator;
        }
    }

    private void ClearEventDropIndicator()
    {
        if (eventDropIndicator is not null)
        {
            eventDropIndicator.ClearValue(TagProperty);
            eventDropIndicator = null;
        }
    }
}
