using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ZeitstrahlStudio.App;
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
                TimelineCardMoveRequest? moveRequest = null;
                var view = new TimelineView
                {
                    Project = project,
                    SelectedEvent = project.Events[25],
                    Orientation = TimelineOrientation.Horizontal,
                    ZoomFactor = 1.25,
                    CompressLargeGaps = true,
                    MoveCardCommand = new RecordingCommand(parameter =>
                        moveRequest = Assert.IsType<TimelineCardMoveRequest>(parameter)),
                };

                var horizontalPixels = Render(view, 900, 560);
                Assert.Contains(horizontalPixels, value => value < 80);
                view.RequestCardMove(project.Events[25].Id, 24.5, -13.25);
                Assert.NotNull(moveRequest);
                Assert.Equal(project.Events[25].Id, moveRequest.EventId);
                Assert.Equal(TimelineOrientation.Horizontal, moveRequest.Orientation);
                Assert.Equal(24.5, moveRequest.HorizontalDelta);
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

    private static byte[] Render(TimelineView view, int width, int height)
    {
        view.Measure(new Size(width, height));
        view.Arrange(new Rect(0, 0, width, height));
        view.UpdateLayout();
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(view);
        var stride = width * 4;
        var pixels = new byte[stride * height];
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

            project.AddEvent(timelineEvent, Timestamp);
        }

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
}
