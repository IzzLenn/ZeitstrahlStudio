using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Shared;

namespace ZeitstrahlStudio.DocumentProcessing;

/// <summary>Führt lokale Dokumentanalysen mit einer festen Obergrenze parallel aus.</summary>
public sealed class BoundedAttachmentAnalysisQueue : IAttachmentAnalysisQueue
{
    private readonly IReadOnlyList<IDocumentAnalyzer> analyzers;
    private readonly IAttachmentAnalysisStore store;
    private readonly int maximumConcurrency;

    public BoundedAttachmentAnalysisQueue(
        IEnumerable<IDocumentAnalyzer> analyzers,
        IAttachmentAnalysisStore store,
        int maximumConcurrency = 2)
    {
        ArgumentNullException.ThrowIfNull(analyzers);
        this.analyzers = analyzers.ToArray();
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        if (maximumConcurrency < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumConcurrency),
                "Die Analyseparallelität muss mindestens eins betragen.");
        }

        this.maximumConcurrency = maximumConcurrency;
    }

    public async Task<IReadOnlyList<AttachmentAnalysisOutcome>> AnalyzeAsync(
        ProjectWorkspace workspace,
        IReadOnlyCollection<Attachment> attachments,
        IProgress<FileOperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(attachments);
        var items = attachments.ToArray();
        var outcomes = new AttachmentAnalysisOutcome[items.Length];
        var completed = 0;
        var successful = 0;
        var failed = 0;

        await Parallel.ForEachAsync(
            Enumerable.Range(0, items.Length),
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = maximumConcurrency,
            },
            async (index, token) =>
            {
                var attachment = items[index];
                var analyzerProgress = progress is null
                    ? null
                    : new ForwardingProgress<DocumentAnalysisProgress>(report =>
                        progress.Report(new FileOperationProgress(
                            $"{attachment.OriginalFileName}: {report.CurrentStep}",
                            Volatile.Read(ref completed),
                            items.Length,
                            Volatile.Read(ref successful),
                            Volatile.Read(ref failed))));
                var result = await AnalyzeOneAsync(
                    workspace,
                    attachment,
                    analyzerProgress,
                    token).ConfigureAwait(false);
                outcomes[index] = new AttachmentAnalysisOutcome(attachment, result);
                if (result.IsSuccess)
                {
                    Interlocked.Increment(ref successful);
                }
                else
                {
                    Interlocked.Increment(ref failed);
                }

                var finished = Interlocked.Increment(ref completed);
                progress?.Report(new FileOperationProgress(
                    attachment.OriginalFileName,
                    finished,
                    items.Length,
                    Volatile.Read(ref successful),
                    Volatile.Read(ref failed)));
            }).ConfigureAwait(false);

        return outcomes;
    }

    private async Task<OperationResult<DocumentAnalysisResult>> AnalyzeOneAsync(
        ProjectWorkspace workspace,
        Attachment attachment,
        IProgress<DocumentAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        var analyzer = analyzers.FirstOrDefault(candidate => candidate.CanAnalyze(attachment.MediaType));
        if (analyzer is null)
        {
            return OperationResult<DocumentAnalysisResult>.Failure(new ApplicationError(
                "DocumentTypeUnsupported",
                $"Der Dateityp von „{attachment.OriginalFileName}“ wird noch nicht analysiert."));
        }

        try
        {
            var localFilePath = ResolveAttachmentPath(workspace.WorkingDirectory, attachment);
            var result = await analyzer.AnalyzeAsync(
                localFilePath,
                workspace.WorkingDirectory,
                progress,
                cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess && result.Value is not null)
            {
                await store.SaveAsync(workspace, attachment, result.Value, cancellationToken)
                    .ConfigureAwait(false);
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or
                UnauthorizedAccessException or
                InvalidDataException or
                System.Data.Common.DbException)
        {
            return OperationResult<DocumentAnalysisResult>.Failure(new ApplicationError(
                "DocumentAnalysisFailed",
                $"Das Dokument „{attachment.OriginalFileName}“ konnte nicht lokal analysiert werden.",
                exception.Message));
        }
    }

    private sealed class ForwardingProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }

    private static string ResolveAttachmentPath(string workingDirectory, Attachment attachment)
    {
        var root = Path.GetFullPath(workingDirectory);
        var candidate = Path.GetFullPath(
            Path.Combine(root, attachment.ProjectRelativePath.Replace('/', Path.DirectorySeparatorChar)));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Der interne Anhangspfad verlässt den Projektarbeitsordner.");
        }

        return candidate;
    }
}
