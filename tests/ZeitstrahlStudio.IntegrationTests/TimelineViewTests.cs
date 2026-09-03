using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ZeitstrahlStudio.App;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class TimelineViewTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Render_DrawsHorizontalAndVerticalViewportOnStaThread()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var project = CreateProject();
                project.SetLayoutPosition(
                    new LayoutPosition(
                        project.Events[25].Id,
                        TimelineOrientation.Horizontal,
                        horizontalOffset: 0,
                        verticalOffset: 0),
                    Timestamp);
                TimelineCardMoveRequest? moveRequest = null;
                var thumbnailService = new RecordingThumbnailService();
                var view = new TimelineView
                {
                    Project = project,
                    Workspace = new ProjectWorkspace(project, Path.GetTempPath(), null, true),
                    ThumbnailService = thumbnailService,
                    SelectedEvent = project.Events[25],
                    Orientation = TimelineOrientation.Horizontal,
                    ZoomFactor = 1.25,
                    CompressLargeGaps = true,
                    CardFontSize = 16,
                    AxisFontSize = 13,
                    MoveCardCommand = new RecordingCommand(parameter =>
                        moveRequest = Assert.IsType<TimelineCardMoveRequest>(parameter)),
                };

                _ = Render(view, 900, 560);
                view.ShowWholeProject();
                var horizontalPixels = Render(view, 900, 560);
                Dispatcher.CurrentDispatcher.Invoke(
                    () => { },
                    DispatcherPriority.ApplicationIdle);
                Assert.Contains(horizontalPixels, value => value < 80);
                Assert.Contains(
                    project.Events[0].Attachments.First().Id,
                    thumbnailService.RequestedAttachmentIds);
                var moveZoom = view.ZoomFactor;
                view.RequestCardMove(project.Events[25].Id, 24.5, -13.25);
                Assert.NotNull(moveRequest);
                Assert.Equal(project.Events[25].Id, moveRequest.EventId);
                Assert.Equal(TimelineOrientation.Horizontal, moveRequest.Orientation);
                Assert.Equal(Math.Round(24.5 / moveZoom, 2), moveRequest.HorizontalDelta);
                Assert.Equal(-13.25, moveRequest.VerticalDelta);
                view.VisibleEventIds = project.Events.Take(10).Select(item => item.Id).ToArray();
                view.CenterSelectionRevision++;
                view.RangeRequest = new TimelineRangeRequest(
                    new DateOnly(1910, 1, 1),
                    new DateOnly(1920, 12, 31),
                    Revision: 1);
                Assert.InRange(view.ZoomFactor, 0.25, 8);
                Assert.True(double.IsFinite(view.HorizontalOffset));
                view.ShowWholeProject();
                view.CenterSelectedEvent();
                Assert.InRange(view.ZoomFactor, 0.25, 8);

                view.Orientation = TimelineOrientation.Vertical;
                view.IsDarkTheme = true;
                view.LayoutRevision++;
                var verticalPixels = Render(view, 900, 560);
                Assert.Contains(verticalPixels, value => value < 80);
                var highlighted = new HighlightedTextBlock
                {
                    HighlightedText = "Quelle: friedliche ⟦Wiedervereinigung⟧",
                };
                Assert.Equal(2, highlighted.Inlines.Count);
                view.ResetView();
                Assert.Equal(1, view.ZoomFactor);
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
    public async Task RequestCardMove_NormalizesOnlyTheTimelineAxisForZoom()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var project = CreateProject();
                TimelineCardMoveRequest? moveRequest = null;
                var view = new TimelineView
                {
                    Project = project,
                    ZoomFactor = 2,
                    MoveCardCommand = new RecordingCommand(parameter =>
                        moveRequest = Assert.IsType<TimelineCardMoveRequest>(parameter)),
                };

                view.Orientation = TimelineOrientation.Horizontal;
                view.RequestCardMove(project.Events[0].Id, 40, -18);
                Assert.NotNull(moveRequest);
                Assert.Equal(20, moveRequest.HorizontalDelta);
                Assert.Equal(-18, moveRequest.VerticalDelta);

                view.Orientation = TimelineOrientation.Vertical;
                view.RequestCardMove(project.Events[0].Id, 40, -18);
                Assert.NotNull(moveRequest);
                Assert.Equal(40, moveRequest.HorizontalDelta);
                Assert.Equal(-9, moveRequest.VerticalDelta);
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
    public async Task ZoomRoundTrip_PreservesManualCardViewportPositionWhenCrossExtentChanges()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var project = TimelineProject.Create(Guid.NewGuid(), "Zoom-Rundlauf", Timestamp);
                for (var index = 0; index < 17; index++)
                {
                    var eventTimestamp = new DateTime(2026, 7, 1).AddHours(index * 6);
                    var timelineEvent = TimelineEvent.Create(
                        Guid.NewGuid(),
                        $"Nahes Ereignis {index + 1}",
                        EventDate.ExactWithTime(
                            DateOnly.FromDateTime(eventTimestamp),
                            TimeOnly.FromDateTime(eventTimestamp)),
                        Timestamp);
                    project.AddEvent(timelineEvent, Timestamp);
                }

                var manuallyPositionedEvent = project.Events[0];
                project.SetLayoutPosition(
                    new LayoutPosition(
                        manuallyPositionedEvent.Id,
                        TimelineOrientation.Horizontal,
                        horizontalOffset: 35,
                        verticalOffset: -20),
                    Timestamp);
                var view = new TimelineView
                {
                    Project = project,
                    Orientation = TimelineOrientation.Horizontal,
                    ZoomFactor = 1,
                };

                _ = Render(view, 900, 560);
                view.ResetView();
                _ = Render(view, 900, 560);
                var initialRect = GetViewportCardRect(view, manuallyPositionedEvent.Id);
                var initialCrossExtent = view.ExtentHeight;

                view.ZoomFactor = 8;
                _ = Render(view, 900, 560);
                Assert.NotEqual(initialCrossExtent, view.ExtentHeight);

                view.ZoomFactor = 1;
                _ = Render(view, 900, 560);
                var restoredRect = GetViewportCardRect(view, manuallyPositionedEvent.Id);

                Assert.Equal(initialRect.X, restoredRect.X, precision: 6);
                Assert.Equal(initialRect.Y, restoredRect.Y, precision: 6);
                Assert.Equal(initialRect.Width, restoredRect.Width, precision: 6);
                Assert.Equal(initialRect.Height, restoredRect.Height, precision: 6);
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

    [Theory]
    [InlineData(TimelineOrientation.Horizontal)]
    [InlineData(TimelineOrientation.Vertical)]
    public async Task FirstCardMoveAtZoom_CommitsThePreviewPositionWithoutLaneJump(
        TimelineOrientation orientation)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var project = TimelineProject.Create(Guid.NewGuid(), "Erster Zoom-Drag", Timestamp);
                project.AddEvent(
                    TimelineEvent.Create(
                        Guid.NewGuid(),
                        "Erstes Ereignis",
                        EventDate.Exact(new DateOnly(2026, 7, 1)),
                        Timestamp),
                    Timestamp);
                project.AddEvent(
                    TimelineEvent.Create(
                        Guid.NewGuid(),
                        "Zweites Ereignis",
                        EventDate.Exact(new DateOnly(2026, 7, 3)),
                        Timestamp),
                    Timestamp);
                project.AddEvent(
                    TimelineEvent.Create(
                        Guid.NewGuid(),
                        "Zu verschiebendes Ereignis",
                        EventDate.Exact(new DateOnly(2026, 7, 5)),
                        Timestamp),
                    Timestamp);
                var target = project.Events[2];
                var editingService = new ProjectEventEditingService();
                var view = new TimelineView
                {
                    Project = project,
                    Orientation = orientation,
                    ZoomFactor = 8,
                };
                view.MoveCardCommand = new RecordingCommand(parameter =>
                {
                    var request = Assert.IsType<TimelineCardMoveRequest>(parameter);
                    editingService.MoveLayoutPosition(
                        project,
                        request.EventId,
                        request.Orientation,
                        request.HorizontalDelta,
                        request.VerticalDelta,
                        Timestamp.AddMinutes(1));
                    view.LayoutRevision++;
                });

                _ = Render(view, 900, 560);
                var initialRect = GetViewportCardRect(view, target.Id);

                view.RequestCardMove(target.Id, horizontalDelta: 40, verticalDelta: -18);
                _ = Render(view, 900, 560);
                var committedRect = GetViewportCardRect(view, target.Id);
                var storedPosition = project.LayoutPositions.Single(
                    position => position.EventId == target.Id && position.Orientation == orientation);

                Assert.Equal(initialRect.X + 40, committedRect.X, precision: 6);
                Assert.Equal(initialRect.Y - 18, committedRect.Y, precision: 6);

                view.ZoomFactor = 1;
                _ = Render(view, 900, 560);
                view.ZoomFactor = 8;
                _ = Render(view, 900, 560);
                var restoredRect = GetViewportCardRect(view, target.Id);

                Assert.Equal(committedRect.X, restoredRect.X, precision: 6);
                Assert.Equal(committedRect.Y, restoredRect.Y, precision: 6);
                Assert.Same(storedPosition, project.LayoutPositions.Single(
                    position => position.EventId == target.Id && position.Orientation == orientation));
                Assert.Equal(new DateTime(2026, 7, 5), target.Date.SortStart);
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

    [Theory]
    [InlineData(TimelineOrientation.Horizontal, 1.0)]
    [InlineData(TimelineOrientation.Vertical, 1.0)]
    [InlineData(TimelineOrientation.Horizontal, 0.75)]
    [InlineData(TimelineOrientation.Vertical, 0.75)]
    public void AxisLabelSelection_KeepsGlobalMonthPhaseWhenViewportMoves(
        TimelineOrientation orientation,
        double zoomFactor)
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Globale Tickphase", Timestamp);
        project.AddEvent(
            TimelineEvent.Create(
                Guid.NewGuid(),
                "Beginn",
                EventDate.Exact(new DateOnly(2024, 1, 1)),
                Timestamp),
            Timestamp);
        project.AddEvent(
            TimelineEvent.Create(
                Guid.NewGuid(),
                "Ende",
                EventDate.Exact(new DateOnly(2027, 1, 1)),
                Timestamp),
            Timestamp);
        var layout = new TimelineLayoutEngine().Create(
            project,
            new TimelineLayoutOptions(
                orientation,
                ZoomFactor: zoomFactor,
                CompressLargeGaps: false));
        var ticks = layout.Ticks.Take(10).ToArray();
        const double minimumLabelSpacing = 96;

        Assert.Equal(TimelineScaleUnit.Months, layout.ScaleUnit);
        Assert.Equal(10, ticks.Length);
        Assert.True(ticks[1].AxisPosition - ticks[0].AxisPosition < minimumLabelSpacing);
        Assert.True(ticks[2].AxisPosition - ticks[0].AxisPosition >= minimumLabelSpacing);

        var labelsBeforePan = TimelineView.SelectVisibleAxisLabelPositions(
            layout.Ticks,
            layout.Breaks,
            minimumLabelSpacing,
            ticks[0].AxisPosition - 1,
            ticks[8].AxisPosition + 1);
        var labelsAfterPan = TimelineView.SelectVisibleAxisLabelPositions(
            layout.Ticks,
            layout.Breaks,
            minimumLabelSpacing,
            ticks[1].AxisPosition - 1,
            ticks[9].AxisPosition + 1);
        var commonStart = ticks[1].AxisPosition;
        var commonEnd = ticks[8].AxisPosition;
        var commonLabelsBeforePan = layout.Ticks
            .Where(tick => tick.AxisPosition >= commonStart && tick.AxisPosition <= commonEnd)
            .Where(tick => labelsBeforePan.Contains(tick.AxisPosition))
            .Select(tick => tick.Value)
            .ToArray();
        var commonLabelsAfterPan = layout.Ticks
            .Where(tick => tick.AxisPosition >= commonStart && tick.AxisPosition <= commonEnd)
            .Where(tick => labelsAfterPan.Contains(tick.AxisPosition))
            .Select(tick => tick.Value)
            .ToArray();

        Assert.Equal(commonLabelsBeforePan, commonLabelsAfterPan);
        Assert.Equal(
            [
                new DateTime(2024, 3, 1),
                new DateTime(2024, 5, 1),
                new DateTime(2024, 7, 1),
                new DateTime(2024, 9, 1),
            ],
            commonLabelsAfterPan);
    }

    [Fact]
    public void AxisLabelSelection_ReservesSpaceForBreakLabelGlobally()
    {
        var ticks = new[]
        {
            new TimelineAxisTick(new DateTime(2024, 1, 1), 0, "Januar 2024", true),
            new TimelineAxisTick(new DateTime(2024, 3, 1), 150, "März 2024", false),
            new TimelineAxisTick(new DateTime(2024, 5, 1), 300, "Mai 2024", false),
        };
        var breaks = new[]
        {
            new TimelineAxisBreak(
                new DateTime(2024, 2, 1),
                new DateTime(2024, 4, 1),
                AxisStart: 140,
                AxisEnd: 160,
                Label: "Unterbrechung"),
        };

        var labels = TimelineView.SelectVisibleAxisLabelPositions(
            ticks,
            breaks,
            minimumLabelSpacing: 96,
            visibleAxisStart: double.NegativeInfinity,
            visibleAxisEnd: double.PositiveInfinity);

        Assert.Equal([0d, 300d], labels.Order());
    }

    [Theory]
    [InlineData(TimelineOrientation.Horizontal)]
    [InlineData(TimelineOrientation.Vertical)]
    public async Task MakeVisible_ForTimelineViewportDoesNotResetPannedOffsets(
        TimelineOrientation orientation)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var view = new TimelineView
                {
                    Project = CreateTwoDimensionScrollableProject(),
                    Orientation = orientation,
                    CompressLargeGaps = false,
                };
                _ = Render(view, 900, 560);
                var expectedHorizontalOffset = Math.Min(
                    300,
                    (view.ExtentWidth - view.ViewportWidth) / 2);
                var expectedVerticalOffset = Math.Min(
                    200,
                    (view.ExtentHeight - view.ViewportHeight) / 2);
                Assert.True(expectedHorizontalOffset > 0);
                Assert.True(expectedVerticalOffset > 0);
                view.SetHorizontalOffset(expectedHorizontalOffset);
                view.SetVerticalOffset(expectedVerticalOffset);

                var visible = view.MakeVisible(view, new Rect(view.RenderSize));

                Assert.True(visible.IsEmpty);
                Assert.Equal(expectedHorizontalOffset, view.HorizontalOffset, precision: 6);
                Assert.Equal(expectedVerticalOffset, view.VerticalOffset, precision: 6);
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

    [Theory]
    [InlineData(TimelineOrientation.Horizontal)]
    [InlineData(TimelineOrientation.Vertical)]
    public async Task TemporaryZeroViewport_DoesNotDiscardPannedOffsets(
        TimelineOrientation orientation)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var view = new TimelineView
                {
                    Project = CreateTwoDimensionScrollableProject(),
                    Orientation = orientation,
                    CompressLargeGaps = false,
                };
                _ = Render(view, 900, 560);
                var expectedHorizontalOffset = Math.Min(
                    300,
                    (view.ExtentWidth - view.ViewportWidth) / 2);
                var expectedVerticalOffset = Math.Min(
                    200,
                    (view.ExtentHeight - view.ViewportHeight) / 2);
                Assert.True(expectedHorizontalOffset > 0);
                Assert.True(expectedVerticalOffset > 0);
                view.SetHorizontalOffset(expectedHorizontalOffset);
                view.SetVerticalOffset(expectedVerticalOffset);

                view.Measure(new Size(0, 0));
                view.Arrange(new Rect(0, 0, 0, 0));
                view.UpdateLayout();
                view.LayoutRevision++;

                _ = Render(view, 900, 560);

                Assert.Equal(900, view.ViewportWidth);
                Assert.Equal(560, view.ViewportHeight);
                Assert.Equal(expectedHorizontalOffset, view.HorizontalOffset, precision: 6);
                Assert.Equal(expectedVerticalOffset, view.VerticalOffset, precision: 6);
                Assert.InRange(view.HorizontalOffset, 0, view.ExtentWidth - view.ViewportWidth);
                Assert.InRange(view.VerticalOffset, 0, view.ExtentHeight - view.ViewportHeight);
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
    public async Task Render_UsesEventColorForTheCompleteSelectedCardFrame()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                const int width = 700;
                const int height = 460;
                var project = TimelineProject.Create(Guid.NewGuid(), "Farbtest", Timestamp);
                var timelineEvent = TimelineEvent.Create(
                    Guid.NewGuid(),
                    "Vollständig farbiger Rahmen",
                    EventDate.Exact(new DateOnly(2026, 7, 27)),
                    Timestamp);
                timelineEvent.SetClassification(
                    EventPriority.Normal,
                    EventStatus.Active,
                    "#12AB34",
                    Timestamp);
                project.AddEvent(timelineEvent, Timestamp);
                var view = new TimelineView
                {
                    Project = project,
                    SelectedEvent = timelineEvent,
                    Orientation = TimelineOrientation.Horizontal,
                    CompressLargeGaps = true,
                };

                _ = Render(view, width, height);
                view.ShowWholeProject();
                var pixels = Render(view, width, height);
                var layoutField = typeof(TimelineView).GetField(
                    "layout",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var layout = Assert.IsType<TimelineLayoutResult>(layoutField!.GetValue(view));
                var card = Assert.Single(layout.Cards);
                var getCardRect = typeof(TimelineView).GetMethod(
                    "GetCardRect",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var cardRect = Assert.IsType<Rect>(getCardRect!.Invoke(view, [card]));
                var framePoint = new Point(
                    cardRect.Left + (cardRect.Width / 2) - view.HorizontalOffset,
                    cardRect.Top + 1 - view.VerticalOffset);

                AssertColorNear(
                    pixels,
                    width,
                    height,
                    framePoint,
                    Color.FromRgb(0x12, 0xAB, 0x34));
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
    [Theory]
    [InlineData(1.00)]
    [InlineData(1.25)]
    [InlineData(1.50)]
    [InlineData(2.00)]
    public async Task Render_DrawsAtSupportedDpiScales(double scale)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var project = CreateProject();
                var view = new TimelineView
                {
                    Project = project,
                    SelectedEvent = project.Events[25],
                    Orientation = TimelineOrientation.Horizontal,
                    CompressLargeGaps = true,
                };

                var pixels = Render(view, 900, 560, scale);

                Assert.Contains(pixels, value => value < 80);
                Assert.True(view.ActualWidth > 0);
                Assert.True(view.ActualHeight > 0);
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

    private static void AssertColorNear(
        byte[] pixels,
        int width,
        int height,
        Point point,
        Color expected)
    {
        var centerX = (int)Math.Round(point.X);
        var centerY = (int)Math.Round(point.Y);
        for (var y = Math.Max(0, centerY - 2); y <= Math.Min(height - 1, centerY + 2); y++)
        {
            for (var x = Math.Max(0, centerX - 2); x <= Math.Min(width - 1, centerX + 2); x++)
            {
                var offset = ((y * width) + x) * 4;
                if (pixels[offset] == expected.B &&
                    pixels[offset + 1] == expected.G &&
                    pixels[offset + 2] == expected.R &&
                    pixels[offset + 3] == byte.MaxValue)
                {
                    return;
                }
            }
        }

        Assert.Fail($"Am Kartenrahmen wurde nicht die Ereignisfarbe {expected} gefunden.");
    }

    private static Rect GetViewportCardRect(TimelineView view, Guid eventId)
    {
        var layoutField = typeof(TimelineView).GetField(
            "layout",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var layout = Assert.IsType<TimelineLayoutResult>(layoutField!.GetValue(view));
        var card = layout.Cards.Single(candidate => candidate.EventId == eventId);
        var getCardRect = typeof(TimelineView).GetMethod(
            "GetCardRect",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var cardRect = Assert.IsType<Rect>(getCardRect!.Invoke(view, [card]));
        cardRect.Offset(-view.HorizontalOffset, -view.VerticalOffset);
        return cardRect;
    }

    private static byte[] Render(TimelineView view, int width, int height, double scale = 1)
    {
        view.Measure(new Size(width, height));
        view.Arrange(new Rect(0, 0, width, height));
        view.UpdateLayout();
        var pixelWidth = checked((int)Math.Round(width * scale));
        var pixelHeight = checked((int)Math.Round(height * scale));
        var dpi = 96 * scale;
        var bitmap = new RenderTargetBitmap(
            pixelWidth,
            pixelHeight,
            dpi,
            dpi,
            PixelFormats.Pbgra32);
        bitmap.Render(view);
        var stride = pixelWidth * 4;
        var pixels = new byte[stride * pixelHeight];
        bitmap.CopyPixels(pixels, stride, 0);
        return pixels;
    }

    private static TimelineProject CreateProject()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "WPF-Zeitstrahl", Timestamp);
        for (var index = 0; index < 100; index++)
        {
            var year = index < 50 ? 1900 + index : 2000 + index;
            var timelineEvent = TimelineEvent.Create(
                Guid.NewGuid(),
                $"Ereignis {index + 1}",
                EventDate.Exact(new DateOnly(year, 7, 19)),
                Timestamp);
            timelineEvent.UpdateContent(
                timelineEvent.Title,
                "Eine kurze sichtbare Ereignisbeschreibung",
                description: null,
                source: null,
                notes: null,
                Timestamp);
            if (index == 25)
            {
                timelineEvent.SetDeadline(
                    new Deadline(
                        Guid.NewGuid(),
                        new DateOnly(year + 2, 1, 15),
                        label: "Testfrist"),
                    Timestamp);
            }

            if (index == 0)
            {
                timelineEvent.AddAttachment(
                    new Attachment(
                        Guid.NewGuid(),
                        "miniatur.png",
                        "image/png",
                        1,
                        new string('a', 64),
                        null,
                        Timestamp,
                        $"attachments/{Guid.NewGuid():N}/miniatur.png"),
                    Timestamp);
            }

            project.AddEvent(timelineEvent, Timestamp);
        }

        return project;
    }

    private static TimelineProject CreateTwoDimensionScrollableProject()
    {
        var project = TimelineProject.Create(Guid.NewGuid(), "Zweiseitig scrollbar", Timestamp);
        for (var index = 0; index < 12; index++)
        {
            project.AddEvent(
                TimelineEvent.Create(
                    Guid.NewGuid(),
                    $"Gleichzeitiges Ereignis {index + 1}",
                    EventDate.Exact(new DateOnly(1900, 1, 1)),
                    Timestamp),
                Timestamp);
        }

        project.AddEvent(
            TimelineEvent.Create(
                Guid.NewGuid(),
                "Spätes Ereignis",
                EventDate.Exact(new DateOnly(2026, 1, 1)),
                Timestamp),
            Timestamp);
        return project;
    }

    private sealed class RecordingCommand(Action<object?> execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute(parameter);
    }

    private sealed class RecordingThumbnailService : ITimelineThumbnailService
    {
        private static readonly byte[] ThumbnailData = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

        public List<Guid> RequestedAttachmentIds { get; } = [];

        public Task<TimelineThumbnail?> GetOrCreateAsync(
            ProjectWorkspace workspace,
            Attachment attachment,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestedAttachmentIds.Add(attachment.Id);
            return Task.FromResult<TimelineThumbnail?>(new TimelineThumbnail(
                attachment.Id,
                1,
                1,
                "thumbnails/timeline/test.png",
                ThumbnailData));
        }
    }
}
