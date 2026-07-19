using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Rein visuelle Verschiebung einer Zeitstrahlkarte in Viewportkoordinaten.</summary>
public sealed record TimelineCardMoveRequest(
    Guid EventId,
    TimelineOrientation Orientation,
    double HorizontalDelta,
    double VerticalDelta);

/// <summary>Ein einmaliger Navigationsauftrag für einen fachlichen Datumsbereich.</summary>
public sealed record TimelineRangeRequest(DateOnly Start, DateOnly End, int Revision);

/// <summary>
/// Viewportbezogen gezeichneter WPF-Zeitstrahl. Es werden keine visuellen Kartenobjekte für
/// außerhalb des sichtbaren Ausschnitts liegende Ereignisse erzeugt.
/// </summary>
public sealed class TimelineView : FrameworkElement, IScrollInfo
{
    private const double LineScrollAmount = 48;
    private const double CardCornerRadius = 8;
    private const int MaximumDecodedThumbnailCount = 128;
    private Brush BackgroundBrush = null!;
    private Brush AxisBrush = null!;
    private Brush MutedTextBrush = null!;
    private Brush PrimaryTextBrush = null!;
    private Brush SelectedBrush = null!;
    private Brush DeadlineBrush = null!;
    private Brush CardBrush = null!;
    private Brush ThumbnailPlaceholderBrush = null!;
    private Pen AxisPen = null!;
    private Pen ConnectorPen = null!;
    private Pen DeadlinePen = null!;
    private readonly TimelineLayoutEngine layoutEngine = new();
    private readonly Dictionary<string, Brush> eventBrushes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ThumbnailCacheKey, CachedThumbnail> thumbnailCache = [];
    private readonly HashSet<ThumbnailCacheKey> loadingThumbnails = [];
    private readonly HashSet<ThumbnailCacheKey> failedThumbnails = [];
    private CancellationTokenSource thumbnailCancellation = new();
    private long thumbnailAccessSequence;
    private int thumbnailGeneration;
    private TimelineLayoutResult? layout;
    private double horizontalOffset;
    private double verticalOffset;
    private double extentWidth;
    private double extentHeight;
    private double viewportWidth;
    private double viewportHeight;
    private Point panStart;
    private double panStartHorizontalOffset;
    private double panStartVerticalOffset;
    private bool isPanning;
    private TimelineCardLayout? draggedCard;
    private Point cardDragStart;
    private Vector cardDragDelta;
    private bool isCardDragActive;
    private Guid? fileDropTargetEventId;
    private int lastAppliedRangeRevision = int.MinValue;

    public TimelineView()
    {
        Focusable = true;
        ClipToBounds = true;
        AllowDrop = true;
        ApplyPalette(isDark: false);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public static readonly DependencyProperty ProjectProperty = DependencyProperty.Register(
        nameof(Project),
        typeof(TimelineProject),
        typeof(TimelineView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnProjectChanged));

    public static readonly DependencyProperty WorkspaceProperty = DependencyProperty.Register(
        nameof(Workspace),
        typeof(ProjectWorkspace),
        typeof(TimelineView),
        new FrameworkPropertyMetadata(null, OnWorkspaceChanged));

    public static readonly DependencyProperty ThumbnailServiceProperty = DependencyProperty.Register(
        nameof(ThumbnailService),
        typeof(ITimelineThumbnailService),
        typeof(TimelineView),
        new FrameworkPropertyMetadata(null, OnThumbnailServiceChanged));

    public static readonly DependencyProperty SelectedEventProperty = DependencyProperty.Register(
        nameof(SelectedEvent),
        typeof(TimelineEvent),
        typeof(TimelineView),
        new FrameworkPropertyMetadata(
            null,
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(TimelineOrientation),
        typeof(TimelineView),
        new FrameworkPropertyMetadata(
            TimelineOrientation.Horizontal,
            FrameworkPropertyMetadataOptions.AffectsRender,
            OnLayoutInputChanged));

    public static readonly DependencyProperty ZoomFactorProperty = DependencyProperty.Register(
        nameof(ZoomFactor),
        typeof(double),
        typeof(TimelineView),
        new FrameworkPropertyMetadata(
            1d,
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnLayoutInputChanged,
            CoerceZoom));

    public static readonly DependencyProperty CompressLargeGapsProperty = DependencyProperty.Register(
        nameof(CompressLargeGaps),
        typeof(bool),
        typeof(TimelineView),
        new FrameworkPropertyMetadata(
            true,
            FrameworkPropertyMetadataOptions.AffectsRender,
            OnLayoutInputChanged));

    public static readonly DependencyProperty LayoutRevisionProperty = DependencyProperty.Register(
        nameof(LayoutRevision),
        typeof(int),
        typeof(TimelineView),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender, OnContentRevisionChanged));

    public static readonly DependencyProperty CardFontSizeProperty = DependencyProperty.Register(
        nameof(CardFontSize),
        typeof(double),
        typeof(TimelineView),
        new FrameworkPropertyMetadata(
            14d,
            FrameworkPropertyMetadataOptions.AffectsRender,
            OnLayoutInputChanged,
            CoerceFontSize));

    public static readonly DependencyProperty AxisFontSizeProperty = DependencyProperty.Register(
        nameof(AxisFontSize),
        typeof(double),
        typeof(TimelineView),
        new FrameworkPropertyMetadata(
            12d,
            FrameworkPropertyMetadataOptions.AffectsRender,
            null,
            CoerceFontSize));

    public static readonly DependencyProperty IsDarkThemeProperty = DependencyProperty.Register(
        nameof(IsDarkTheme),
        typeof(bool),
        typeof(TimelineView),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnThemeChanged));

    public static readonly DependencyProperty MoveCardCommandProperty = DependencyProperty.Register(
        nameof(MoveCardCommand),
        typeof(ICommand),
        typeof(TimelineView),
        new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty DropFilesCommandProperty = DependencyProperty.Register(
        nameof(DropFilesCommand),
        typeof(ICommand),
        typeof(TimelineView),
        new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty RangeRequestProperty = DependencyProperty.Register(
        nameof(RangeRequest),
        typeof(TimelineRangeRequest),
        typeof(TimelineView),
        new FrameworkPropertyMetadata(null, OnRangeRequestChanged));

    public static readonly DependencyProperty VisibleEventIdsProperty = DependencyProperty.Register(
        nameof(VisibleEventIds),
        typeof(IReadOnlyCollection<Guid>),
        typeof(TimelineView),
        new FrameworkPropertyMetadata(null, OnLayoutInputChanged));

    public static readonly DependencyProperty CenterSelectionRevisionProperty = DependencyProperty.Register(
        nameof(CenterSelectionRevision),
        typeof(int),
        typeof(TimelineView),
        new FrameworkPropertyMetadata(0, OnCenterSelectionRequested));

    public TimelineProject? Project
    {
        get => (TimelineProject?)GetValue(ProjectProperty);
        set => SetValue(ProjectProperty, value);
    }

    public ProjectWorkspace? Workspace
    {
        get => (ProjectWorkspace?)GetValue(WorkspaceProperty);
        set => SetValue(WorkspaceProperty, value);
    }

    public ITimelineThumbnailService? ThumbnailService
    {
        get => (ITimelineThumbnailService?)GetValue(ThumbnailServiceProperty);
        set => SetValue(ThumbnailServiceProperty, value);
    }

    public TimelineEvent? SelectedEvent
    {
        get => (TimelineEvent?)GetValue(SelectedEventProperty);
        set => SetValue(SelectedEventProperty, value);
    }

    public TimelineOrientation Orientation
    {
        get => (TimelineOrientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public double ZoomFactor
    {
        get => (double)GetValue(ZoomFactorProperty);
        set => SetValue(ZoomFactorProperty, value);
    }

    public bool CompressLargeGaps
    {
        get => (bool)GetValue(CompressLargeGapsProperty);
        set => SetValue(CompressLargeGapsProperty, value);
    }

    public int LayoutRevision
    {
        get => (int)GetValue(LayoutRevisionProperty);
        set => SetValue(LayoutRevisionProperty, value);
    }

    public double CardFontSize
    {
        get => (double)GetValue(CardFontSizeProperty);
        set => SetValue(CardFontSizeProperty, value);
    }

    public double AxisFontSize
    {
        get => (double)GetValue(AxisFontSizeProperty);
        set => SetValue(AxisFontSizeProperty, value);
    }

    public bool IsDarkTheme
    {
        get => (bool)GetValue(IsDarkThemeProperty);
        set => SetValue(IsDarkThemeProperty, value);
    }

    public ICommand? MoveCardCommand
    {
        get => (ICommand?)GetValue(MoveCardCommandProperty);
        set => SetValue(MoveCardCommandProperty, value);
    }

    public ICommand? DropFilesCommand
    {
        get => (ICommand?)GetValue(DropFilesCommandProperty);
        set => SetValue(DropFilesCommandProperty, value);
    }

    public TimelineRangeRequest? RangeRequest
    {
        get => (TimelineRangeRequest?)GetValue(RangeRequestProperty);
        set => SetValue(RangeRequestProperty, value);
    }

    public IReadOnlyCollection<Guid>? VisibleEventIds
    {
        get => (IReadOnlyCollection<Guid>?)GetValue(VisibleEventIdsProperty);
        set => SetValue(VisibleEventIdsProperty, value);
    }

    public int CenterSelectionRevision
    {
        get => (int)GetValue(CenterSelectionRevisionProperty);
        set => SetValue(CenterSelectionRevisionProperty, value);
    }

    public bool CanHorizontallyScroll { get; set; } = true;
    public bool CanVerticallyScroll { get; set; } = true;
    public double ExtentWidth => extentWidth;
    public double ExtentHeight => extentHeight;
    public double ViewportWidth => viewportWidth;
    public double ViewportHeight => viewportHeight;
    public double HorizontalOffset => horizontalOffset;
    public double VerticalOffset => verticalOffset;
    public ScrollViewer? ScrollOwner { get; set; }

    public void ZoomIn() => SetCurrentValue(ZoomFactorProperty, ZoomFactor * 1.2);

    public void ZoomOut() => SetCurrentValue(ZoomFactorProperty, ZoomFactor / 1.2);

    public void ResetView()
    {
        SetCurrentValue(ZoomFactorProperty, 1d);
        RebuildLayout();
        CenterCrossAxis();
        SetPrimaryOffset(0);
    }

    public void ShowWholeProject()
    {
        if (Project is null || Project.Events.Count == 0)
        {
            ResetView();
            return;
        }

        var axisViewport = Orientation == TimelineOrientation.Horizontal ? viewportWidth : viewportHeight;
        var baseline = layoutEngine.Create(
            Project,
            CreateOptions(zoomFactor: 1),
            VisibleEventIds?.ToHashSet());
        var baselineAxis = baseline.ContentAxisLength;
        var zoom = Math.Clamp((axisViewport - 24) / Math.Max(1, baselineAxis), 0.25, 8);
        SetCurrentValue(ZoomFactorProperty, zoom);
        RebuildLayout();
        CenterCrossAxis();
        SetPrimaryOffset(0);
    }

    public void CenterSelectedEvent()
    {
        if (layout is null || SelectedEvent is null)
        {
            return;
        }

        var card = layout.Cards.FirstOrDefault(item => item.EventId == SelectedEvent.Id);
        if (card is null)
        {
            return;
        }

        if (Orientation == TimelineOrientation.Horizontal)
        {
            SetHorizontalOffset(card.AxisPosition - (viewportWidth / 2));
            SetVerticalOffset(GetCrossCenter() + card.CrossPosition - (viewportHeight / 2));
        }
        else
        {
            SetVerticalOffset(card.AxisPosition - (viewportHeight / 2));
            SetHorizontalOffset(GetCrossCenter() + card.CrossPosition - (viewportWidth / 2));
        }
    }

    public void ShowRange(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new ArgumentException("Das Ende des sichtbaren Zeitraums darf nicht vor dessen Beginn liegen.");
        }

        _ = TryShowRange(start, end);
    }

    /// <summary>Leitet eine visuelle Kartenverschiebung an das gebundene ViewModel weiter.</summary>
    public void RequestCardMove(
        Guid eventId,
        double horizontalDelta,
        double verticalDelta)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Die Kartenverschiebung benötigt eine Ereignis-ID.", nameof(eventId));
        }

        if (!double.IsFinite(horizontalDelta) || !double.IsFinite(verticalDelta))
        {
            throw new ArgumentException("Die Kartenverschiebung muss endliche Werte enthalten.");
        }

        if (Math.Abs(horizontalDelta) < 1 && Math.Abs(verticalDelta) < 1)
        {
            return;
        }

        var request = new TimelineCardMoveRequest(
            eventId,
            Orientation,
            Math.Round(horizontalDelta, 2),
            Math.Round(verticalDelta, 2));
        if (MoveCardCommand?.CanExecute(request) == true)
        {
            MoveCardCommand.Execute(request);
        }
    }

    public void LineUp() => SetVerticalOffset(VerticalOffset - LineScrollAmount);
    public void LineDown() => SetVerticalOffset(VerticalOffset + LineScrollAmount);
    public void LineLeft() => SetHorizontalOffset(HorizontalOffset - LineScrollAmount);
    public void LineRight() => SetHorizontalOffset(HorizontalOffset + LineScrollAmount);
    public void MouseWheelUp() => LineUp();
    public void MouseWheelDown() => LineDown();
    public void MouseWheelLeft() => LineLeft();
    public void MouseWheelRight() => LineRight();
    public void PageUp() => SetVerticalOffset(VerticalOffset - ViewportHeight);
    public void PageDown() => SetVerticalOffset(VerticalOffset + ViewportHeight);
    public void PageLeft() => SetHorizontalOffset(HorizontalOffset - ViewportWidth);
    public void PageRight() => SetHorizontalOffset(HorizontalOffset + ViewportWidth);

    public Rect MakeVisible(Visual visual, Rect rectangle)
    {
        if (visual == this)
        {
            SetHorizontalOffset(rectangle.Left);
            SetVerticalOffset(rectangle.Top);
        }

        return rectangle;
    }

    public void SetHorizontalOffset(double offset)
    {
        var coerced = CoerceOffset(offset, extentWidth, viewportWidth);
        if (Math.Abs(coerced - horizontalOffset) < 0.1)
        {
            return;
        }

        horizontalOffset = coerced;
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateVisual();
    }

    public void SetVerticalOffset(double offset)
    {
        var coerced = CoerceOffset(offset, extentHeight, viewportHeight);
        if (Math.Abs(coerced - verticalOffset) < 0.1)
        {
            return;
        }

        verticalOffset = coerced;
        ScrollOwner?.InvalidateScrollInfo();
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsFinite(availableSize.Width) ? availableSize.Width : 0;
        var height = double.IsFinite(availableSize.Height) ? availableSize.Height : 0;
        UpdateViewport(width, height);
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        UpdateViewport(finalSize.Width, finalSize.Height);
        return finalSize;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        drawingContext.DrawRectangle(BackgroundBrush, null, new Rect(RenderSize));
        if (layout is null || Project is null || layout.Cards.Count == 0)
        {
            DrawEmptyState(drawingContext);
            return;
        }

        drawingContext.PushClip(new RectangleGeometry(new Rect(RenderSize)));
        drawingContext.PushTransform(new TranslateTransform(-horizontalOffset, -verticalOffset));
        DrawAxis(drawingContext);
        DrawConnectionsAndDeadlines(drawingContext);
        DrawVisibleCards(drawingContext);
        drawingContext.Pop();
        drawingContext.Pop();
    }

    protected override void OnDragOver(DragEventArgs e)
    {
        base.OnDragOver(e);
        if (!TryGetDroppedPaths(e.Data, out var paths))
        {
            ClearFileDropTarget();
            return;
        }

        var target = HitTestCard(e.GetPosition(this));
        if (target is null)
        {
            ClearFileDropTarget();
            return;
        }

        var request = new AttachmentDropRequest(target.EventId, paths);
        var canExecute = DropFilesCommand?.CanExecute(request) == true;
        SetFileDropTarget(canExecute ? target.EventId : null);
        e.Effects = canExecute ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    protected override void OnDragLeave(DragEventArgs e)
    {
        base.OnDragLeave(e);
        ClearFileDropTarget();
    }

    protected override void OnDrop(DragEventArgs e)
    {
        base.OnDrop(e);
        if (!TryGetDroppedPaths(e.Data, out var paths))
        {
            ClearFileDropTarget();
            return;
        }

        var target = HitTestCard(e.GetPosition(this));
        if (target is null)
        {
            ClearFileDropTarget();
            return;
        }

        var request = new AttachmentDropRequest(target.EventId, paths);
        if (DropFilesCommand?.CanExecute(request) == true)
        {
            DropFilesCommand.Execute(request);
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        ClearFileDropTarget();
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        var hit = HitTestCard(e.GetPosition(this));
        if (hit is not null && Project is not null)
        {
            var timelineEvent = Project.Events.FirstOrDefault(item => item.Id == hit.EventId);
            if (timelineEvent is not null)
            {
                SetCurrentValue(SelectedEventProperty, timelineEvent);
            }

            BeginCardDrag(hit, e.GetPosition(this));
            e.Handled = true;
            return;
        }

        BeginPan(e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.ChangedButton == MouseButton.Middle)
        {
            Focus();
            BeginPan(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (draggedCard is not null)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                CancelCardDrag();
                return;
            }

            var dragPoint = e.GetPosition(this);
            var delta = dragPoint - cardDragStart;
            if (!isCardDragActive &&
                (Math.Abs(delta.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(delta.Y) >= SystemParameters.MinimumVerticalDragDistance))
            {
                isCardDragActive = true;
                Cursor = Cursors.SizeAll;
            }

            if (isCardDragActive)
            {
                cardDragDelta = delta;
                InvalidateVisual();
            }

            e.Handled = true;
            return;
        }

        if (!isPanning || e.LeftButton != MouseButtonState.Pressed && e.MiddleButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(this);
        SetHorizontalOffset(panStartHorizontalOffset - (current.X - panStart.X));
        SetVerticalOffset(panStartVerticalOffset - (current.Y - panStart.Y));
        e.Handled = true;
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (draggedCard is not null && e.ChangedButton == MouseButton.Left)
        {
            CompleteCardDrag();
            e.Handled = true;
            return;
        }

        if (isPanning)
        {
            isPanning = false;
            ReleaseMouseCapture();
            Cursor = Cursors.Arrow;
            e.Handled = true;
        }
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        if (draggedCard is not null)
        {
            ClearCardDrag();
        }

        if (isPanning)
        {
            isPanning = false;
            Cursor = Cursors.Arrow;
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        var pointer = e.GetPosition(this);
        var oldZoom = ZoomFactor;
        var newZoom = Math.Clamp(oldZoom * (e.Delta > 0 ? 1.15 : 1 / 1.15), 0.25, 8);
        if (Math.Abs(newZoom - oldZoom) < 0.0001)
        {
            return;
        }

        var oldAxisCoordinate = Orientation == TimelineOrientation.Horizontal
            ? horizontalOffset + pointer.X
            : verticalOffset + pointer.Y;
        SetCurrentValue(ZoomFactorProperty, newZoom);
        var newAxisCoordinate = 180 + ((oldAxisCoordinate - 180) * (newZoom / oldZoom));
        if (Orientation == TimelineOrientation.Horizontal)
        {
            SetHorizontalOffset(newAxisCoordinate - pointer.X);
        }
        else
        {
            SetVerticalOffset(newAxisCoordinate - pointer.Y);
        }

        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.Key)
        {
            case Key.Add:
            case Key.OemPlus:
                ZoomIn();
                e.Handled = true;
                break;
            case Key.Subtract:
            case Key.OemMinus:
                ZoomOut();
                e.Handled = true;
                break;
            case Key.Home:
                ShowWholeProject();
                e.Handled = true;
                break;
        }
    }

    private static void OnLayoutInputChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var view = (TimelineView)dependencyObject;
        view.RebuildLayout();
    }

    private static void OnProjectChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var view = (TimelineView)dependencyObject;
        var previousId = (e.OldValue as TimelineProject)?.Id;
        var currentId = (e.NewValue as TimelineProject)?.Id;
        if (previousId != currentId)
        {
            view.ResetThumbnailState(clearCache: true);
        }

        view.RebuildLayout();
    }

    private static void OnWorkspaceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var view = (TimelineView)dependencyObject;
        view.ResetThumbnailState(clearCache: true);
        view.InvalidateVisual();
    }

    private static void OnThumbnailServiceChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var view = (TimelineView)dependencyObject;
        view.ResetThumbnailState(clearCache: true);
        view.InvalidateVisual();
    }

    private static void OnContentRevisionChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var view = (TimelineView)dependencyObject;
        view.failedThumbnails.Clear();
        view.RebuildLayout();
    }

    private static void OnThemeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var view = (TimelineView)dependencyObject;
        view.ApplyPalette((bool)e.NewValue);
        view.InvalidateVisual();
    }

    private static void OnRangeRequestChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var view = (TimelineView)dependencyObject;
        view.TryApplyRangeRequest();
    }

    private static void OnCenterSelectionRequested(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var view = (TimelineView)dependencyObject;
        view.CenterSelectedEvent();
    }

    private static object CoerceZoom(DependencyObject dependencyObject, object baseValue)
    {
        var value = (double)baseValue;
        return double.IsFinite(value) ? Math.Clamp(value, 0.25, 8) : 1d;
    }

    private static object CoerceFontSize(DependencyObject dependencyObject, object baseValue)
    {
        var value = (double)baseValue;
        return double.IsFinite(value) ? Math.Clamp(value, 8, 48) : 14d;
    }

    private void RebuildLayout()
    {
        if (Project is null || viewportWidth <= 0 || viewportHeight <= 0)
        {
            layout = null;
            UpdateExtent(viewportWidth, viewportHeight);
            InvalidateVisual();
            return;
        }

        layout = layoutEngine.Create(
            Project,
            CreateOptions(ZoomFactor),
            VisibleEventIds?.ToHashSet());
        var width = Orientation == TimelineOrientation.Horizontal
            ? layout.ContentAxisLength
            : layout.ContentCrossLength;
        var height = Orientation == TimelineOrientation.Horizontal
            ? layout.ContentCrossLength
            : layout.ContentAxisLength;
        UpdateExtent(width, height);
        InvalidateVisual();
    }

    private TimelineLayoutOptions CreateOptions(double zoomFactor)
    {
        var axisLength = Orientation == TimelineOrientation.Horizontal ? viewportWidth : viewportHeight;
        var crossLength = Orientation == TimelineOrientation.Horizontal ? viewportHeight : viewportWidth;
        return new TimelineLayoutOptions(
            Orientation,
            zoomFactor,
            CompressLargeGaps,
            Math.Max(1, axisLength),
            Math.Max(1, crossLength),
            CardFontSize);
    }

    private void UpdateViewport(double width, double height)
    {
        if (Math.Abs(width - viewportWidth) < 0.1 && Math.Abs(height - viewportHeight) < 0.1)
        {
            return;
        }

        viewportWidth = Math.Max(0, width);
        viewportHeight = Math.Max(0, height);
        RebuildLayout();
        TryApplyRangeRequest();
        ScrollOwner?.InvalidateScrollInfo();
    }

    private void UpdateExtent(double width, double height)
    {
        extentWidth = Math.Max(viewportWidth, width);
        extentHeight = Math.Max(viewportHeight, height);
        horizontalOffset = CoerceOffset(horizontalOffset, extentWidth, viewportWidth);
        verticalOffset = CoerceOffset(verticalOffset, extentHeight, viewportHeight);
        ScrollOwner?.InvalidateScrollInfo();
    }

    private void DrawAxis(DrawingContext drawingContext)
    {
        var crossCenter = GetCrossCenter();
        if (Orientation == TimelineOrientation.Horizontal)
        {
            drawingContext.DrawLine(AxisPen, new Point(24, crossCenter), new Point(extentWidth - 24, crossCenter));
        }
        else
        {
            drawingContext.DrawLine(AxisPen, new Point(crossCenter, 24), new Point(crossCenter, extentHeight - 24));
        }

        var visible = GetVisibleContentRect(80);
        foreach (var tick in layout!.Ticks)
        {
            if (!IsAxisPositionVisible(tick.AxisPosition, visible))
            {
                continue;
            }

            DrawTick(drawingContext, tick, crossCenter);
        }

        foreach (var axisBreak in layout.Breaks)
        {
            if (!IsAxisPositionVisible((axisBreak.AxisStart + axisBreak.AxisEnd) / 2, visible))
            {
                continue;
            }

            DrawBreak(drawingContext, axisBreak, crossCenter);
        }
    }

    private void DrawTick(DrawingContext drawingContext, TimelineAxisTick tick, double crossCenter)
    {
        var length = tick.IsMajor ? 9 : 6;
        if (Orientation == TimelineOrientation.Horizontal)
        {
            drawingContext.DrawLine(
                ConnectorPen,
                new Point(tick.AxisPosition, crossCenter - length),
                new Point(tick.AxisPosition, crossCenter + length));
            DrawText(drawingContext, tick.Label, AxisFontSize, MutedTextBrush,
                new Point(tick.AxisPosition - 48, crossCenter + 12), 96, AxisFontSize * 1.8, TextAlignment.Center, false);
        }
        else
        {
            drawingContext.DrawLine(
                ConnectorPen,
                new Point(crossCenter - length, tick.AxisPosition),
                new Point(crossCenter + length, tick.AxisPosition));
            DrawText(drawingContext, tick.Label, AxisFontSize, MutedTextBrush,
                new Point(crossCenter + 12, tick.AxisPosition - (AxisFontSize * 0.9)), 104, AxisFontSize * 1.8, TextAlignment.Left, false);
        }
    }

    private void DrawBreak(DrawingContext drawingContext, TimelineAxisBreak axisBreak, double crossCenter)
    {
        var middle = (axisBreak.AxisStart + axisBreak.AxisEnd) / 2;
        var backgroundPen = CreatePen(BackgroundBrush, 14);
        if (Orientation == TimelineOrientation.Horizontal)
        {
            drawingContext.DrawLine(backgroundPen, new Point(middle - 16, crossCenter), new Point(middle + 16, crossCenter));
            var points = new StreamGeometry();
            using (var context = points.Open())
            {
                context.BeginFigure(new Point(middle - 14, crossCenter - 8), false, false);
                context.PolyLineTo(
                    [
                        new Point(middle - 7, crossCenter + 8),
                        new Point(middle, crossCenter - 8),
                        new Point(middle + 7, crossCenter + 8),
                        new Point(middle + 14, crossCenter - 8),
                    ],
                    true,
                    false);
            }

            points.Freeze();
            drawingContext.DrawGeometry(null, CreatePen(DeadlineBrush, 2), points);
            DrawText(drawingContext, axisBreak.Label, Math.Max(8, AxisFontSize * 0.9), DeadlineBrush,
                new Point(middle - 105, crossCenter - 31), 210, AxisFontSize * 1.8, TextAlignment.Center, true);
        }
        else
        {
            drawingContext.DrawLine(backgroundPen, new Point(crossCenter, middle - 16), new Point(crossCenter, middle + 16));
            var points = new StreamGeometry();
            using (var context = points.Open())
            {
                context.BeginFigure(new Point(crossCenter - 8, middle - 14), false, false);
                context.PolyLineTo(
                    [
                        new Point(crossCenter + 8, middle - 7),
                        new Point(crossCenter - 8, middle),
                        new Point(crossCenter + 8, middle + 7),
                        new Point(crossCenter - 8, middle + 14),
                    ],
                    true,
                    false);
            }

            points.Freeze();
            drawingContext.DrawGeometry(null, CreatePen(DeadlineBrush, 2), points);
            DrawText(drawingContext, axisBreak.Label, Math.Max(8, AxisFontSize * 0.9), DeadlineBrush,
                new Point(crossCenter + 24, middle - 10), 210, AxisFontSize * 1.8, TextAlignment.Left, true);
        }
    }

    private void DrawConnectionsAndDeadlines(DrawingContext drawingContext)
    {
        var visible = GetVisibleContentRect(160);
        foreach (var card in layout!.Cards)
        {
            var rect = GetCardRect(card);
            if (!rect.IntersectsWith(visible))
            {
                continue;
            }

            var axisPoint = GetAxisPoint(card.AnchorAxisPosition);
            var cardPoint = GetCardConnectorPoint(rect);
            drawingContext.DrawLine(ConnectorPen, axisPoint, cardPoint);
        }

        foreach (var deadline in layout.Deadlines)
        {
            if (!IsAxisPositionVisible(deadline.AxisPosition, visible) &&
                !IsAxisPositionVisible(deadline.EventAxisPosition, visible))
            {
                continue;
            }

            drawingContext.DrawLine(
                DeadlinePen,
                GetAxisPoint(deadline.EventAxisPosition),
                GetAxisPoint(deadline.AxisPosition));
            DrawDeadlineDiamond(drawingContext, deadline);
        }
    }

    private void DrawDeadlineDiamond(DrawingContext drawingContext, TimelineDeadlineLayout deadline)
    {
        var center = GetAxisPoint(deadline.AxisPosition);
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(center.X, center.Y - 7), true, true);
            context.PolyLineTo(
                [
                    new Point(center.X + 7, center.Y),
                    new Point(center.X, center.Y + 7),
                    new Point(center.X - 7, center.Y),
                ],
                true,
                true);
        }

        geometry.Freeze();
        drawingContext.DrawGeometry(DeadlineBrush, CreatePen(CreateBrush("#92400E"), 1), geometry);
        DrawText(
            drawingContext,
            deadline.Label,
            10,
            DeadlineBrush,
            Orientation == TimelineOrientation.Horizontal
                ? new Point(center.X - 70, center.Y - 31)
                : new Point(center.X + 14, center.Y - 10),
            140,
            20,
            Orientation == TimelineOrientation.Horizontal ? TextAlignment.Center : TextAlignment.Left,
            true);
    }

    private void DrawVisibleCards(DrawingContext drawingContext)
    {
        var visible = GetVisibleContentRect(40);
        var events = Project!.Events.ToDictionary(timelineEvent => timelineEvent.Id);
        foreach (var card in layout!.Cards)
        {
            var rect = GetCardRect(card);
            if (!rect.IntersectsWith(visible) || !events.TryGetValue(card.EventId, out var timelineEvent))
            {
                continue;
            }

            DrawCard(drawingContext, rect, card, timelineEvent);
        }
    }

    private void DrawCard(
        DrawingContext drawingContext,
        Rect rect,
        TimelineCardLayout card,
        TimelineEvent timelineEvent)
    {
        var color = GetEventBrush(timelineEvent.ColorHex);
        var selected = SelectedEvent?.Id == timelineEvent.Id;
        var isFileDropTarget = fileDropTargetEventId == timelineEvent.Id;
        var borderPen = CreatePen(
            selected || isFileDropTarget ? SelectedBrush : color,
            isFileDropTarget ? 4 : selected ? 3 : 2);
        drawingContext.DrawRoundedRectangle(
            CardBrush,
            borderPen,
            rect,
            CardCornerRadius,
            CardCornerRadius);
        var colorBar = Orientation == TimelineOrientation.Horizontal
            ? new Rect(rect.Left, rect.Top, 7, rect.Height)
            : new Rect(rect.Left, rect.Top, rect.Width, 7);
        drawingContext.DrawRoundedRectangle(color, null, colorBar, 3, 3);
        var padding = Math.Max(10, CardFontSize * 0.85);
        var left = rect.Left + padding;
        var top = rect.Top + padding;
        var right = rect.Right - padding;
        var dateFontSize = Math.Max(8, CardFontSize * 0.78);
        var bodyFontSize = Math.Max(8, CardFontSize * 0.78);
        var badgeFontSize = Math.Max(8, CardFontSize * 0.7);
        var dateHeight = dateFontSize * 1.45;
        var titleTop = top + dateHeight + 3;
        var titleHeight = CardFontSize * 2.5;
        var badgeHeight = badgeFontSize * 1.5;
        var badgeTop = rect.Bottom - padding - badgeHeight;
        var bodyTop = titleTop + titleHeight + 3;
        var bodyHeight = Math.Max(0, badgeTop - bodyTop - 3);
        var primaryAttachment = TimelineThumbnailSelection.SelectPrimary(timelineEvent);
        var thumbnailWidth = Math.Min(96, Math.Max(58, rect.Width * 0.3));
        var thumbnailHeight = Math.Min(72, Math.Max(44, rect.Height * 0.42));
        var thumbnailRect = new Rect(
            right - thumbnailWidth,
            titleTop,
            thumbnailWidth,
            thumbnailHeight);
        var textRight = primaryAttachment is null
            ? right
            : thumbnailRect.Left - Math.Max(8, padding * 0.55);
        var textWidth = Math.Max(1, textRight - left);

        DrawText(drawingContext, timelineEvent.Date.ToDisplayString(), dateFontSize, color,
            new Point(left, top), right - left, dateHeight, TextAlignment.Left, true);
        DrawText(drawingContext, timelineEvent.Title, CardFontSize, PrimaryTextBrush,
            new Point(left, titleTop), textWidth, titleHeight, TextAlignment.Left, true);
        DrawText(drawingContext, timelineEvent.InfoText ?? string.Empty, bodyFontSize, MutedTextBrush,
            new Point(left, bodyTop), textWidth, bodyHeight, TextAlignment.Left, false);

        if (primaryAttachment is not null)
        {
            DrawOrQueueThumbnail(
                drawingContext,
                thumbnailRect,
                timelineEvent,
                primaryAttachment);
        }

        var badges = new List<string> { GetPriorityText(timelineEvent.Priority) };
        if (timelineEvent.Deadline is not null)
        {
            badges.Add("Frist");
        }

        if (timelineEvent.Attachments.Count > 0)
        {
            badges.Add($"{timelineEvent.Attachments.Count} Anhänge");
        }

        if (card.HasManualPosition)
        {
            badges.Add("manuell");
        }

        DrawText(drawingContext, string.Join("  ·  ", badges), badgeFontSize, MutedTextBrush,
            new Point(left, badgeTop), right - left, badgeHeight, TextAlignment.Left, false);
    }

    private void DrawOrQueueThumbnail(
        DrawingContext drawingContext,
        Rect destination,
        TimelineEvent timelineEvent,
        Attachment attachment)
    {
        var project = Project;
        if (project is null)
        {
            return;
        }

        var key = ThumbnailCacheKey.Create(project.Id, attachment);
        drawingContext.DrawRoundedRectangle(
            ThumbnailPlaceholderBrush,
            CreatePen(AxisBrush, 1),
            destination,
            4,
            4);
        if (TryGetCachedThumbnail(key, out var thumbnail))
        {
            var scale = Math.Min(
                destination.Width / thumbnail.PixelWidth,
                destination.Height / thumbnail.PixelHeight);
            var width = thumbnail.PixelWidth * scale;
            var height = thumbnail.PixelHeight * scale;
            var fitted = new Rect(
                destination.Left + ((destination.Width - width) / 2),
                destination.Top + ((destination.Height - height) / 2),
                width,
                height);
            drawingContext.DrawImage(thumbnail.Image, fitted);
            return;
        }

        DrawText(
            drawingContext,
            failedThumbnails.Contains(key) ? "Keine Vorschau" : "Vorschau …",
            8,
            MutedTextBrush,
            new Point(destination.Left + 3, destination.Top + ((destination.Height - 16) / 2)),
            Math.Max(1, destination.Width - 6),
            16,
            TextAlignment.Center,
            false);
        QueueThumbnailLoad(key, timelineEvent, attachment);
    }

    private bool TryGetCachedThumbnail(
        ThumbnailCacheKey key,
        out CachedThumbnail thumbnail)
    {
        if (!thumbnailCache.TryGetValue(key, out thumbnail!))
        {
            return false;
        }

        thumbnail = thumbnail with { LastAccess = ++thumbnailAccessSequence };
        thumbnailCache[key] = thumbnail;
        return true;
    }

    private void QueueThumbnailLoad(
        ThumbnailCacheKey key,
        TimelineEvent timelineEvent,
        Attachment attachment)
    {
        if (ThumbnailService is null || Workspace is null ||
            Workspace.Project.Id != Project?.Id ||
            failedThumbnails.Contains(key) ||
            !loadingThumbnails.Add(key))
        {
            return;
        }

        var generation = thumbnailGeneration;
        var workspace = Workspace;
        var service = ThumbnailService;
        var token = thumbnailCancellation.Token;
        _ = Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => _ = LoadThumbnailAsync(
                generation,
                key,
                workspace,
                timelineEvent,
                attachment,
                service,
                token)));
    }

    private async Task LoadThumbnailAsync(
        int generation,
        ThumbnailCacheKey key,
        ProjectWorkspace workspace,
        TimelineEvent timelineEvent,
        Attachment attachment,
        ITimelineThumbnailService service,
        CancellationToken cancellationToken)
    {
        try
        {
            if (TimelineThumbnailSelection.SelectPrimary(timelineEvent)?.Id != attachment.Id)
            {
                return;
            }

            var result = await service.GetOrCreateAsync(
                workspace,
                attachment,
                cancellationToken).ConfigureAwait(true);
            if (generation != thumbnailGeneration || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (result is null)
            {
                failedThumbnails.Add(key);
                return;
            }

            var image = DecodeThumbnail(result.EncodedImageData);
            thumbnailCache[key] = new CachedThumbnail(
                image,
                result.PixelWidth,
                result.PixelHeight,
                ++thumbnailAccessSequence);
            TrimThumbnailCache();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidDataException or
            InvalidOperationException or ArgumentException)
        {
            if (generation == thumbnailGeneration)
            {
                failedThumbnails.Add(key);
            }
        }
        finally
        {
            if (generation == thumbnailGeneration)
            {
                loadingThumbnails.Remove(key);
                InvalidateVisual();
            }
        }
    }

    private void TrimThumbnailCache()
    {
        while (thumbnailCache.Count > MaximumDecodedThumbnailCount)
        {
            var oldest = thumbnailCache.MinBy(item => item.Value.LastAccess);
            thumbnailCache.Remove(oldest.Key);
        }
    }

    private static BitmapImage DecodeThumbnail(byte[] data)
    {
        using var stream = new MemoryStream(data, writable: false);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private TimelineCardLayout? HitTestCard(Point viewportPoint)
    {
        if (layout is null)
        {
            return null;
        }

        var contentPoint = new Point(
            viewportPoint.X + horizontalOffset,
            viewportPoint.Y + verticalOffset);
        return layout.Cards
            .Reverse()
            .FirstOrDefault(card => GetCardRect(card).Contains(contentPoint));
    }

    private Rect GetCardRect(TimelineCardLayout card)
    {
        var crossCenter = GetCrossCenter();
        var rect = Orientation == TimelineOrientation.Horizontal
            ? new Rect(
                card.AxisPosition - (card.AxisLength / 2),
                crossCenter + card.CrossPosition - (card.CrossLength / 2),
                card.AxisLength,
                card.CrossLength)
            : new Rect(
                crossCenter + card.CrossPosition - (card.CrossLength / 2),
                card.AxisPosition - (card.AxisLength / 2),
                card.CrossLength,
                card.AxisLength);
        if (isCardDragActive && draggedCard?.EventId == card.EventId)
        {
            rect.Offset(cardDragDelta.X, cardDragDelta.Y);
        }

        return rect;
    }

    private Point GetAxisPoint(double axisPosition)
    {
        var crossCenter = GetCrossCenter();
        return Orientation == TimelineOrientation.Horizontal
            ? new Point(axisPosition, crossCenter)
            : new Point(crossCenter, axisPosition);
    }

    private Point GetCardConnectorPoint(Rect rect)
    {
        var crossCenter = GetCrossCenter();
        return Orientation == TimelineOrientation.Horizontal
            ? new Point(
                rect.Left + (rect.Width / 2),
                rect.Top + (rect.Height / 2) >= crossCenter ? rect.Top : rect.Bottom)
            : new Point(
                rect.Left + (rect.Width / 2) >= crossCenter ? rect.Left : rect.Right,
                rect.Top + (rect.Height / 2));
    }

    private Rect GetVisibleContentRect(double margin) => new(
        horizontalOffset - margin,
        verticalOffset - margin,
        viewportWidth + (margin * 2),
        viewportHeight + (margin * 2));

    private bool IsAxisPositionVisible(double value, Rect visible) =>
        Orientation == TimelineOrientation.Horizontal
            ? value >= visible.Left && value <= visible.Right
            : value >= visible.Top && value <= visible.Bottom;

    private double GetCrossCenter() => Orientation == TimelineOrientation.Horizontal
        ? extentHeight / 2
        : extentWidth / 2;

    private void CenterCrossAxis()
    {
        if (Orientation == TimelineOrientation.Horizontal)
        {
            SetVerticalOffset(GetCrossCenter() - (viewportHeight / 2));
        }
        else
        {
            SetHorizontalOffset(GetCrossCenter() - (viewportWidth / 2));
        }
    }

    private void SetPrimaryOffset(double value)
    {
        if (Orientation == TimelineOrientation.Horizontal)
        {
            SetHorizontalOffset(value);
        }
        else
        {
            SetVerticalOffset(value);
        }
    }

    private void BeginPan(Point point)
    {
        panStart = point;
        panStartHorizontalOffset = horizontalOffset;
        panStartVerticalOffset = verticalOffset;
        isPanning = true;
        Cursor = Cursors.Hand;
        CaptureMouse();
    }

    private void BeginCardDrag(TimelineCardLayout card, Point point)
    {
        draggedCard = card;
        cardDragStart = point;
        cardDragDelta = default;
        isCardDragActive = false;
        CaptureMouse();
    }

    private void CompleteCardDrag()
    {
        var card = draggedCard;
        var delta = cardDragDelta;
        var shouldCommit = isCardDragActive && card is not null;
        ClearCardDrag();
        ReleaseMouseCapture();
        if (shouldCommit)
        {
            RequestCardMove(card!.EventId, delta.X, delta.Y);
        }
    }

    private void CancelCardDrag()
    {
        ClearCardDrag();
        ReleaseMouseCapture();
    }

    private void ClearCardDrag()
    {
        draggedCard = null;
        cardDragDelta = default;
        isCardDragActive = false;
        Cursor = Cursors.Arrow;
        InvalidateVisual();
    }

    private void TryApplyRangeRequest()
    {
        if (RangeRequest is not { } request || request.Revision == lastAppliedRangeRevision)
        {
            return;
        }

        if (TryShowRange(request.Start, request.End))
        {
            lastAppliedRangeRevision = request.Revision;
        }
    }

    private bool TryShowRange(DateOnly start, DateOnly end)
    {
        if (Project is null || Project.Events.Count == 0 ||
            viewportWidth <= 0 || viewportHeight <= 0 || end < start)
        {
            return false;
        }

        var axisViewport = Orientation == TimelineOrientation.Horizontal ? viewportWidth : viewportHeight;
        var startValue = start.ToDateTime(TimeOnly.MinValue);
        var endValue = end.ToDateTime(TimeOnly.MaxValue);
        var baselineOptions = CreateOptions(1);
        var baselineStart = layoutEngine.GetAxisPosition(Project, baselineOptions, startValue);
        var baselineEnd = layoutEngine.GetAxisPosition(Project, baselineOptions, endValue);
        var zoom = Math.Clamp(
            (axisViewport - 48) / Math.Max(1, Math.Abs(baselineEnd - baselineStart)),
            0.25,
            8);

        double mappedStart = baselineStart;
        double mappedEnd = baselineEnd;
        for (var iteration = 0; iteration < 2; iteration++)
        {
            SetCurrentValue(ZoomFactorProperty, zoom);
            RebuildLayout();
            var options = CreateOptions(ZoomFactor);
            mappedStart = layoutEngine.GetAxisPosition(Project, options, startValue);
            mappedEnd = layoutEngine.GetAxisPosition(Project, options, endValue);
            var actualSpan = Math.Max(1, Math.Abs(mappedEnd - mappedStart));
            var adjusted = Math.Clamp(zoom * ((axisViewport - 48) / actualSpan), 0.25, 8);
            if (Math.Abs(adjusted - zoom) < 0.01)
            {
                break;
            }

            zoom = adjusted;
        }

        CenterCrossAxis();
        SetPrimaryOffset(Math.Min(mappedStart, mappedEnd) - 24);
        return true;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (thumbnailCancellation.IsCancellationRequested)
        {
            thumbnailCancellation.Dispose();
            thumbnailCancellation = new CancellationTokenSource();
            thumbnailGeneration++;
            loadingThumbnails.Clear();
        }

        InvalidateVisual();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ClearFileDropTarget();
        thumbnailCancellation.Cancel();
        thumbnailGeneration++;
        loadingThumbnails.Clear();
    }

    private void ResetThumbnailState(bool clearCache)
    {
        thumbnailCancellation.Cancel();
        thumbnailCancellation.Dispose();
        thumbnailCancellation = new CancellationTokenSource();
        thumbnailGeneration++;
        loadingThumbnails.Clear();
        failedThumbnails.Clear();
        if (clearCache)
        {
            thumbnailCache.Clear();
            thumbnailAccessSequence = 0;
        }
    }

    private void ApplyPalette(bool isDark)
    {
        BackgroundBrush = CreateBrush(isDark ? "#0F172A" : "#F8FAFC");
        AxisBrush = CreateBrush(isDark ? "#64748B" : "#94A3B8");
        MutedTextBrush = CreateBrush(isDark ? "#CBD5E1" : "#64748B");
        PrimaryTextBrush = CreateBrush(isDark ? "#F8FAFC" : "#0F172A");
        SelectedBrush = CreateBrush(isDark ? "#60A5FA" : "#2563EB");
        DeadlineBrush = CreateBrush(isDark ? "#F87171" : "#DC2626");
        CardBrush = CreateBrush(isDark ? "#1E293B" : "#FFFFFF");
        ThumbnailPlaceholderBrush = CreateBrush(isDark ? "#334155" : "#E2E8F0");
        AxisPen = CreatePen(AxisBrush, 2);
        ConnectorPen = CreatePen(AxisBrush, 1);
        DeadlinePen = CreateDashedPen(DeadlineBrush, 1.5);
    }

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

    private void SetFileDropTarget(Guid? eventId)
    {
        if (fileDropTargetEventId == eventId)
        {
            return;
        }

        fileDropTargetEventId = eventId;
        InvalidateVisual();
    }

    private void ClearFileDropTarget() => SetFileDropTarget(null);

    private void DrawEmptyState(DrawingContext drawingContext)
    {
        DrawText(
            drawingContext,
            Project is null
                ? "Kein Projekt geöffnet"
                : VisibleEventIds is not null
                    ? "Keine Ereignisse entsprechen den aktiven Filtern"
                    : "Noch keine Ereignisse für den Zeitstrahl vorhanden",
            CardFontSize,
            MutedTextBrush,
            new Point(20, Math.Max(20, (RenderSize.Height / 2) - 12)),
            Math.Max(1, RenderSize.Width - 40),
            28,
            TextAlignment.Center,
            false);
    }

    private void DrawText(
        DrawingContext drawingContext,
        string text,
        double fontSize,
        Brush brush,
        Point origin,
        double maximumWidth,
        double maximumHeight,
        TextAlignment alignment,
        bool semibold)
    {
        if (string.IsNullOrWhiteSpace(text) || maximumWidth <= 0 || maximumHeight <= 0)
        {
            return;
        }

        var formatted = new FormattedText(
            text,
            CultureInfo.GetCultureInfo("de-DE"),
            FlowDirection.LeftToRight,
            new Typeface(
                new FontFamily("Segoe UI"),
                FontStyles.Normal,
                semibold ? FontWeights.SemiBold : FontWeights.Normal,
                FontStretches.Normal),
            fontSize,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip)
        {
            MaxTextWidth = maximumWidth,
            MaxTextHeight = maximumHeight,
            TextAlignment = alignment,
            Trimming = TextTrimming.CharacterEllipsis,
        };
        drawingContext.DrawText(formatted, origin);
    }

    private Brush GetEventBrush(string colorHex)
    {
        if (eventBrushes.TryGetValue(colorHex, out var brush))
        {
            return brush;
        }

        brush = CreateBrush(colorHex);
        eventBrushes[colorHex] = brush;
        return brush;
    }

    private static string GetPriorityText(EventPriority priority) => priority switch
    {
        EventPriority.Low => "Niedrig",
        EventPriority.Normal => "Normal",
        EventPriority.High => "Hoch",
        EventPriority.Critical => "Kritisch",
        _ => "Priorität",
    };

    private static double CoerceOffset(double value, double extent, double viewport)
    {
        if (!double.IsFinite(value))
        {
            return 0;
        }

        return Math.Clamp(value, 0, Math.Max(0, extent - viewport));
    }

    private static Brush CreateBrush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }

    private static Pen CreatePen(Brush brush, double thickness)
    {
        var pen = new Pen(brush, thickness);
        pen.Freeze();
        return pen;
    }

    private static Pen CreateDashedPen(Brush brush, double thickness)
    {
        var pen = new Pen(brush, thickness)
        {
            DashStyle = DashStyles.Dash,
        };
        pen.Freeze();
        return pen;
    }

    private sealed record ThumbnailCacheKey(
        Guid ProjectId,
        Guid AttachmentId,
        string Sha256,
        int LinkedPage)
    {
        public static ThumbnailCacheKey Create(Guid projectId, Attachment attachment) => new(
            projectId,
            attachment.Id,
            attachment.Sha256,
            attachment.MediaType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                ? attachment.LinkedPdfPage ?? 1
                : 0);
    }

    private sealed record CachedThumbnail(
        BitmapSource Image,
        int PixelWidth,
        int PixelHeight,
        long LastAccess);
}
