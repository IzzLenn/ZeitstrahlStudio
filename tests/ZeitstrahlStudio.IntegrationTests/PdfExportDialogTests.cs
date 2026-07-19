using System.Windows;
using System.Windows.Threading;
using ZeitstrahlStudio.App;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class PdfExportDialogTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 7, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Dialog_InitializesRealPreviewBindingsOnStaThread()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(async () =>
        {
            try
            {
                var project = TimelineProject.Create(Guid.NewGuid(), "Dialogtest", Timestamp);
                project.AddEvent(
                    TimelineEvent.Create(
                        Guid.NewGuid(),
                        "Vorschauereignis",
                        EventDate.Exact(new DateOnly(2026, 7, 19)),
                        Timestamp),
                    Timestamp);
                var workspace = new ProjectWorkspace(
                    project,
                    Path.GetTempPath(),
                    null,
                    false);
                using var viewModel = new PdfExportDialogViewModel(
                    new FakePdfExportService(),
                    new FakePdfPreviewService(),
                    new NullLogService(),
                    workspace,
                    () => null);

                await viewModel.InitializeAsync(CancellationToken.None);

                Assert.True(viewModel.PreviewReady);
                Assert.Equal("2 Seiten · 210 × 297 mm", viewModel.PageSummary);
                Assert.True(viewModel.HasWarnings);
                Assert.Equal("Seite 1 von 2", viewModel.Preview.PageDisplay);
                var dialog = new PdfExportDialog(viewModel);
                dialog.Measure(new Size(1_100, 760));
                dialog.Arrange(new Rect(0, 0, 1_100, 760));
                dialog.UpdateLayout();
                Assert.NotNull(dialog.FindName("PreviewViewport"));
                dialog.Close();
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

    private sealed class FakePdfExportService : IPdfExportService
    {
        public Task<ExportPreview> CreatePreviewAsync(
            ProjectWorkspace workspace,
            PdfExportOptions options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new ExportPreview(
                2,
                210,
                297,
                ["Testwarnung"]));
        }

        public Task ExportAsync(
            ProjectWorkspace workspace,
            PdfExportOptions options,
            string targetPath,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            File.WriteAllBytes(targetPath, "%PDF-test"u8.ToArray());
            return Task.CompletedTask;
        }
    }

    private sealed class FakePdfPreviewService : IPdfPreviewService
    {
        private static readonly byte[] PngData = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAFElEQVR4nGP4z8DAwMDAxMDAwMAAAAwAAf4CB0kAAAAASUVORK5CYII=");

        public Task<PdfPagePreview> RenderPageAsync(
            string validatedLocalPath,
            int pageNumber,
            double renderScale,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new PdfPagePreview(
                pageNumber,
                2,
                200,
                280,
                renderScale,
                PngData));
        }
    }

    private sealed class NullLogService : ILocalLogService
    {
        public Task WriteAsync(LocalLogEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<LocalLogEntry>> ReadRecentAsync(
            int maximumEntries,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LocalLogEntry>>([]);

        public Task ExportAsync(string targetPath, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
