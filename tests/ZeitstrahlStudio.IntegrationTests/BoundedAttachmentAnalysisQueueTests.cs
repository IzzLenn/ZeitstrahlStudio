using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.DocumentProcessing;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Shared;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class BoundedAttachmentAnalysisQueueTests
{
    private const string MediaType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    [Fact]
    public async Task AnalyzeAsync_LimitsConcurrencyPreservesOrderAndPersistsSuccesses()
    {
        var analyzer = new DelayedAnalyzer();
        var store = new RecordingStore();
        var queue = new BoundedAttachmentAnalysisQueue([analyzer], store, maximumConcurrency: 2);
        var attachments = Enumerable.Range(1, 6).Select(CreateAttachment).ToArray();
        var workspace = CreateWorkspace();

        var outcomes = await queue.AnalyzeAsync(
            workspace,
            attachments,
            progress: null,
            CancellationToken.None);

        Assert.Equal(attachments.Select(item => item.Id), outcomes.Select(item => item.Attachment.Id));
        Assert.All(outcomes, outcome => Assert.True(outcome.Result.IsSuccess));
        Assert.Equal(2, analyzer.MaximumObservedConcurrency);
        Assert.Equal(attachments.Length, store.SaveCount);
    }

    [Fact]
    public async Task AnalyzeAsync_PropagatesCancellationWithoutSavingIncompleteResult()
    {
        var analyzer = new BlockingAnalyzer();
        var store = new RecordingStore();
        var queue = new BoundedAttachmentAnalysisQueue([analyzer], store, maximumConcurrency: 1);
        using var cancellation = new CancellationTokenSource();

        var analysisTask = queue.AnalyzeAsync(
            CreateWorkspace(),
            [CreateAttachment(1)],
            progress: null,
            cancellation.Token);
        await analyzer.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => analysisTask);
        Assert.Equal(0, store.SaveCount);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsExplicitFailureForUnsupportedMediaType()
    {
        var store = new RecordingStore();
        var queue = new BoundedAttachmentAnalysisQueue([], store);
        var attachment = CreateAttachment(1) with { };

        var outcomes = await queue.AnalyzeAsync(
            CreateWorkspace(),
            [attachment],
            progress: null,
            CancellationToken.None);

        var outcome = Assert.Single(outcomes);
        Assert.False(outcome.Result.IsSuccess);
        Assert.Equal("DocumentTypeUnsupported", outcome.Result.Error!.Code);
        Assert.Equal(0, store.SaveCount);
    }

    private static ProjectWorkspace CreateWorkspace()
    {
        var project = TimelineProject.Create(
            Guid.NewGuid(),
            "Analysewarteschlange",
            new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero));
        return new ProjectWorkspace(project, Path.GetTempPath(), null, true);
    }

    private static Attachment CreateAttachment(int number) => new(
        Guid.NewGuid(),
        $"Dokument-{number}.docx",
        MediaType,
        number,
        new string('a', 64),
        null,
        new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero),
        $"attachments/{number}.docx");

    private sealed class DelayedAnalyzer : IDocumentAnalyzer
    {
        private int active;
        private int maximumObservedConcurrency;

        public int MaximumObservedConcurrency => Volatile.Read(ref maximumObservedConcurrency);

        public bool CanAnalyze(string mediaType) => mediaType == MediaType;

        public async Task<OperationResult<DocumentAnalysisResult>> AnalyzeAsync(
            string localFilePath,
            string workingDirectory,
            IProgress<DocumentAnalysisProgress>? progress,
            CancellationToken cancellationToken)
        {
            var current = Interlocked.Increment(ref active);
            UpdateMaximum(current);
            try
            {
                await Task.Delay(60, cancellationToken);
                return OperationResult<DocumentAnalysisResult>.Success(new DocumentAnalysisResult(
                    MediaType,
                    Path.GetFileName(localFilePath),
                    "Testinhalt",
                    TextExtractionMethod.OfficeDocument,
                    new Dictionary<string, string>(),
                    [],
                    null,
                    null));
            }
            finally
            {
                Interlocked.Decrement(ref active);
            }
        }

        private void UpdateMaximum(int value)
        {
            var observed = Volatile.Read(ref maximumObservedConcurrency);
            while (value > observed)
            {
                var previous = Interlocked.CompareExchange(
                    ref maximumObservedConcurrency,
                    value,
                    observed);
                if (previous == observed)
                {
                    return;
                }

                observed = previous;
            }
        }
    }

    private sealed class BlockingAnalyzer : IDocumentAnalyzer
    {
        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool CanAnalyze(string mediaType) => mediaType == MediaType;

        public async Task<OperationResult<DocumentAnalysisResult>> AnalyzeAsync(
            string localFilePath,
            string workingDirectory,
            IProgress<DocumentAnalysisProgress>? progress,
            CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Nicht erreichbar.");
        }
    }

    private sealed class RecordingStore : IAttachmentAnalysisStore
    {
        private int saveCount;

        public int SaveCount => Volatile.Read(ref saveCount);

        public Task SaveAsync(
            ProjectWorkspace workspace,
            Attachment attachment,
            DocumentAnalysisResult result,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref saveCount);
            return Task.CompletedTask;
        }

        public Task<DocumentAnalysisResult?> LoadAsync(
            ProjectWorkspace workspace,
            Attachment attachment,
            CancellationToken cancellationToken) =>
            Task.FromResult<DocumentAnalysisResult?>(null);
    }
}
