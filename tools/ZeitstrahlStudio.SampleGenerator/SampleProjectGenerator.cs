using Microsoft.Data.Sqlite;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.DocumentProcessing;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Export;
using ZeitstrahlStudio.Infrastructure;
using ZeitstrahlStudio.Shared;

namespace ZeitstrahlStudio.SampleGenerator;

public sealed record SampleGenerationResult(
    string ArchivePath,
    IReadOnlyList<string> DocumentPaths,
    Guid ProjectId,
    int EventCount);

/// <summary>
/// Erzeugt das frei erfundene Beispielprojekt ausschließlich über produktive lokale Dienste.
/// </summary>
public sealed class SampleProjectGenerator
{
    public const string ArchiveFileName = "ZeitstrahlStudio-Beispiel.zeitprojekt";
    public const string DocumentDirectoryName = "test-documents";

    public async Task<SampleGenerationResult> GenerateAsync(
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);
        var documentDirectory = Path.Combine(fullOutputDirectory, DocumentDirectoryName);
        var documents = await SampleDocumentFactory
            .CreateAsync(documentDirectory, cancellationToken)
            .ConfigureAwait(false);

        var temporaryRoot = Path.Combine(
            Path.GetTempPath(),
            "ZeitstrahlStudio.SampleGenerator",
            Guid.NewGuid().ToString("N"));
        var workingDirectory = Path.Combine(temporaryRoot, "workspace");
        Directory.CreateDirectory(workingDirectory);
        foreach (var name in new[] { "attachments", "thumbnails", "extracted-text", "logs", "metadata" })
        {
            Directory.CreateDirectory(Path.Combine(workingDirectory, name));
        }

        var repository = new SqliteProjectRepository();
        var timeProvider = new FixedTimeProvider(SampleProjectDefinition.GeneratedAtUtc);
        var archiveService = new ProjectArchiveService(repository, timeProvider);
        var archivePath = Path.Combine(fullOutputDirectory, ArchiveFileName);
        try
        {
            var project = SampleProjectDefinition.CreateProject();
            var attachments = await ImportAndAttachDocumentsAsync(
                project,
                documents,
                workingDirectory,
                timeProvider,
                cancellationToken).ConfigureAwait(false);
            await repository.SaveAsync(
                project,
                Path.Combine(workingDirectory, "project.db"),
                cancellationToken).ConfigureAwait(false);

            var workspace = new ProjectWorkspace(
                project,
                workingDirectory,
                archivePath,
                HasUnsavedChanges: false);
            await AnalyzeDocumentsAsync(
                workspace,
                attachments,
                timeProvider,
                cancellationToken).ConfigureAwait(false);
            await CreateImageThumbnailAsync(workspace, attachments, cancellationToken)
                .ConfigureAwait(false);
            await WriteAuditEntriesAsync(workspace, attachments.Count, cancellationToken)
                .ConfigureAwait(false);

            await archiveService.ExportAsync(
                workingDirectory,
                archivePath,
                progress: null,
                cancellationToken).ConfigureAwait(false);
            await ValidateArchiveAsync(
                archiveService,
                archivePath,
                Path.Combine(temporaryRoot, "validated"),
                cancellationToken).ConfigureAwait(false);

            return new SampleGenerationResult(
                archivePath,
                documents.All,
                project.Id,
                project.Events.Count);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(temporaryRoot))
            {
                Directory.Delete(temporaryRoot, recursive: true);
            }
        }
    }

    private static async Task<IReadOnlyList<Attachment>> ImportAndAttachDocumentsAsync(
        TimelineProject project,
        SampleDocumentSet documents,
        string workingDirectory,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var importer = new LocalAttachmentImportService(timeProvider);
        var editor = new ProjectEventEditingService();
        var imported = new List<Attachment>();
        var batches = new[]
        {
            (
                EventId: SampleProjectDefinition.PlanningNorthId,
                Paths: (IReadOnlyCollection<string>)
                [
                    documents.ProjectKickoffPdf,
                    documents.MeetingMinutesPdf,
                ]),
            (
                EventId: SampleProjectDefinition.PlanningSouthId,
                Paths: (IReadOnlyCollection<string>)[documents.PlanningBoardPng]),
            (
                EventId: SampleProjectDefinition.WorkshopNoteId,
                Paths: (IReadOnlyCollection<string>)[documents.WorkshopNoteDocx]),
            (
                EventId: SampleProjectDefinition.MilestonePlanId,
                Paths: (IReadOnlyCollection<string>)[documents.MilestonesXlsx]),
        };

        foreach (var batch in batches)
        {
            var results = await importer.ImportAsync(
                batch.EventId,
                batch.Paths,
                workingDirectory,
                progress: null,
                cancellationToken).ConfigureAwait(false);
            var failures = results.Where(result => !result.IsSuccess).ToArray();
            if (failures.Length > 0)
            {
                throw new InvalidDataException(
                    failures[0].Error?.UserMessage ?? "Ein Beispieldokument konnte nicht importiert werden.");
            }

            var sanitized = results
                .Select(result => RemoveMachineSpecificSourcePath(result.Value!))
                .ToArray();
            editor.AddAttachments(
                project,
                batch.EventId,
                sanitized,
                SampleProjectDefinition.GeneratedAtUtc);
            imported.AddRange(sanitized);
        }

        return imported;
    }

    private static Attachment RemoveMachineSpecificSourcePath(Attachment attachment) => new(
        attachment.Id,
        attachment.OriginalFileName,
        attachment.MediaType,
        attachment.FileSize,
        attachment.Sha256,
        originalSourcePath: null,
        attachment.ImportedAtUtc,
        attachment.ProjectRelativePath,
        attachment.State,
        attachment.LinkedPdfPage);

    private static async Task AnalyzeDocumentsAsync(
        ProjectWorkspace workspace,
        IReadOnlyList<Attachment> attachments,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var store = new SqliteAttachmentAnalysisStore(timeProvider);
        var queue = new BoundedAttachmentAnalysisQueue(
        [
            new DocxDocumentAnalyzer(),
            new XlsxDocumentAnalyzer(),
            new PdfDocumentAnalyzer(new UnexpectedOcrService(), new UnexpectedPdfPreviewService()),
        ],
            store,
            maximumConcurrency: 2);
        var analyzable = attachments
            .Where(attachment => attachment.MediaType is
                "application/pdf" or
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" or
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            .ToArray();
        var outcomes = await queue.AnalyzeAsync(
            workspace,
            analyzable,
            progress: null,
            cancellationToken).ConfigureAwait(false);
        var failed = outcomes.FirstOrDefault(outcome => !outcome.Result.IsSuccess);
        if (failed is not null)
        {
            throw new InvalidDataException(
                failed.Result.Error?.TechnicalDetails ??
                failed.Result.Error?.UserMessage ??
                "Die Analyse eines Beispieldokuments ist fehlgeschlagen.");
        }

        var editor = new ProjectEventEditingService();
        foreach (var timelineEvent in workspace.Project.Events.ToArray())
        {
            var states = outcomes
                .Where(outcome => timelineEvent.Attachments.Any(
                    attachment => attachment.Id == outcome.Attachment.Id))
                .ToDictionary(
                    outcome => outcome.Attachment.Id,
                    _ => AttachmentState.Ready);
            if (states.Count > 0)
            {
                editor.UpdateAttachmentStates(
                    workspace.Project,
                    timelineEvent.Id,
                    states,
                    SampleProjectDefinition.GeneratedAtUtc);
            }
        }

        await new SqliteProjectRepository().SaveAsync(
            workspace.Project,
            Path.Combine(workspace.WorkingDirectory, "project.db"),
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task CreateImageThumbnailAsync(
        ProjectWorkspace workspace,
        IReadOnlyList<Attachment> attachments,
        CancellationToken cancellationToken)
    {
        var image = attachments.Single(attachment => attachment.MediaType == "image/png");
        using var thumbnails = new SkiaTimelineThumbnailService(
            new LocalAttachmentFileService(),
            new UnexpectedPdfPreviewService(),
            new NullLocalLogService());
        var thumbnail = await thumbnails
            .GetOrCreateAsync(workspace, image, cancellationToken)
            .ConfigureAwait(false);
        if (thumbnail is null || thumbnail.EncodedImageData.Length == 0)
        {
            throw new InvalidDataException(
                "Für die Bild-Projektkopie wurde keine Beispielminiatur erzeugt.");
        }
    }

    private static async Task WriteAuditEntriesAsync(
        ProjectWorkspace workspace,
        int attachmentCount,
        CancellationToken cancellationToken)
    {
        var audit = new SqliteAuditLogService();
        var entries = new[]
        {
            new AuditEntry(
                Guid.Parse("a1000000-0000-4000-8000-000000000401"),
                SampleProjectDefinition.GeneratedAtUtc.AddMinutes(-3),
                "Create",
                nameof(TimelineProject),
                workspace.Project.Id,
                "Frei erfundenes Beispielprojekt erzeugt",
                true,
                null),
            new AuditEntry(
                Guid.Parse("a1000000-0000-4000-8000-000000000402"),
                SampleProjectDefinition.GeneratedAtUtc.AddMinutes(-2),
                "AttachmentAdd",
                nameof(Attachment),
                null,
                $"{attachmentCount} lokale Beispieldokumente hinzugefügt und geprüft",
                true,
                null),
            new AuditEntry(
                Guid.Parse("a1000000-0000-4000-8000-000000000403"),
                SampleProjectDefinition.GeneratedAtUtc.AddMinutes(-1),
                "LayoutMove",
                nameof(LayoutPosition),
                SampleProjectDefinition.PlanningNorthId,
                "Beispielhafte manuelle Kartenposition gespeichert",
                true,
                null),
        };
        foreach (var entry in entries)
        {
            await audit.WriteAsync(workspace, entry, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task ValidateArchiveAsync(
        ProjectArchiveService archiveService,
        string archivePath,
        string targetWorkingDirectory,
        CancellationToken cancellationToken)
    {
        var imported = await archiveService.ImportAsync(
            archivePath,
            targetWorkingDirectory,
            progress: null,
            cancellationToken).ConfigureAwait(false);
        if (!imported.IsSuccess || imported.Value is null)
        {
            throw new InvalidDataException(
                imported.Error?.TechnicalDetails ??
                imported.Error?.UserMessage ??
                "Das erzeugte Beispielarchiv konnte nicht erneut geöffnet werden.");
        }

        var workspace = imported.Value.Workspace;
        ValidateProjectContract(workspace.Project);
        var attachments = workspace.Project.Events.SelectMany(timelineEvent => timelineEvent.Attachments).ToArray();
        if (attachments.Any(attachment => attachment.OriginalSourcePath is not null))
        {
            throw new InvalidDataException(
                "Das Beispielarchiv enthält einen maschinenspezifischen Ursprungsdateipfad.");
        }

        var analysisStore = new SqliteAttachmentAnalysisStore();
        var expectedTerms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Projektauftakt.pdf"] = "Kupferstern",
            ["Werkstattnotiz.docx"] = "Morgenfalter",
            ["Meilensteine.xlsx"] = "Blattgold",
        };
        foreach (var pair in expectedTerms)
        {
            var attachment = attachments.Single(item => item.OriginalFileName == pair.Key);
            var result = await analysisStore
                .LoadAsync(workspace, attachment, cancellationToken)
                .ConfigureAwait(false);
            if (result is null ||
                !result.ExtractedText.Contains(pair.Value, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"Der extrahierte Prüfbegriff „{pair.Value}“ fehlt im Beispieldokument „{pair.Key}“.");
            }
        }

        var search = new SqliteProjectSearchService();
        foreach (var term in expectedTerms.Values)
        {
            var results = await search.SearchAsync(
                workspace,
                new SearchCriteria(Query: term),
                cancellationToken).ConfigureAwait(false);
            if (results.Count != 1)
            {
                throw new InvalidDataException(
                    $"Der Dokumentprüfbegriff „{term}“ liefert im Beispielprojekt nicht genau einen Treffer.");
            }
        }

        var thumbnailRoot = Path.Combine(targetWorkingDirectory, "thumbnails", "timeline");
        if (!Directory.Exists(thumbnailRoot) ||
            !Directory.EnumerateFiles(thumbnailRoot, "*.jpg").Any())
        {
            throw new InvalidDataException("Das Beispielarchiv enthält keine erzeugte Dokumentminiatur.");
        }

        var auditEntries = await new SqliteAuditLogService()
            .ReadAsync(workspace, cancellationToken)
            .ConfigureAwait(false);
        if (auditEntries.Count < 3)
        {
            throw new InvalidDataException("Das Beispielarchiv enthält kein vollständiges Beispiel-Audit.");
        }
    }

    internal static void ValidateProjectContract(TimelineProject project)
    {
        if (project.Id != SampleProjectDefinition.ProjectId || project.Events.Count != 10)
        {
            throw new InvalidDataException("Das Beispielprojekt besitzt nicht die erwartete Identität und Ereigniszahl.");
        }

        var precisions = project.Events.Select(timelineEvent => timelineEvent.Date.Precision).ToHashSet();
        if (!Enum.GetValues<DatePrecision>().All(precisions.Contains))
        {
            throw new InvalidDataException("Das Beispielprojekt deckt nicht alle Datumsgenauigkeiten ab.");
        }

        if (!project.Events
            .GroupBy(timelineEvent => timelineEvent.Date)
            .Any(group => group.Count() >= 2))
        {
            throw new InvalidDataException("Im Beispielprojekt fehlen Ereignisse mit identischer Datumsangabe.");
        }

        if (project.Events.Count(timelineEvent => timelineEvent.Deadline is not null) < 3 ||
            project.Events.Select(timelineEvent => timelineEvent.ColorHex).Distinct().Count() < 4 ||
            project.Events.Select(timelineEvent => timelineEvent.Priority).Distinct().Count() < 3 ||
            project.Events.SelectMany(timelineEvent => timelineEvent.Tags).Distinct().Count() < 8 ||
            !project.Events.SelectMany(timelineEvent => timelineEvent.WebLinks).Any())
        {
            throw new InvalidDataException(
                "Fristen, Farben, Prioritäten, Schlagwörter oder Webseitenlink sind unvollständig.");
        }

        var chronological = project.GetChronologicalEvents();
        if (!chronological
            .Zip(chronological.Skip(1), (first, second) =>
                (second.Date.SortStart - first.Date.SortStart).TotalDays)
            .Any(days => days > 20 * 365))
        {
            throw new InvalidDataException("Das Beispielprojekt enthält keine deutlich große Zeitlücke.");
        }

        var mediaTypes = project.Events
            .SelectMany(timelineEvent => timelineEvent.Attachments)
            .Select(attachment => attachment.MediaType)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requiredMediaTypes = new[]
        {
            "application/pdf",
            "image/png",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        };
        if (!requiredMediaTypes.All(mediaTypes.Contains))
        {
            throw new InvalidDataException("Im Beispielprojekt fehlt mindestens ein geforderter Dokumenttyp.");
        }

        if (project.LayoutPositions.Count < 2)
        {
            throw new InvalidDataException("Das Beispielprojekt enthält keine manuellen Layoutbeispiele.");
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset timestamp) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => timestamp;
    }

    private sealed class UnexpectedOcrService : ILocalOcrService
    {
        public Task<LocalOcrResult> RecognizeFileAsync(
            string localFilePath,
            IProgress<DocumentAnalysisProgress>? progress,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException(
                "Die textbasierten Beispiel-PDFs dürfen keine OCR benötigen.");

        public Task<LocalOcrResult> RecognizePngAsync(
            byte[] pngData,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException(
                "Die textbasierten Beispiel-PDFs dürfen keine OCR benötigen.");
    }

    private sealed class UnexpectedPdfPreviewService : IPdfPreviewService
    {
        public Task<PdfPagePreview> RenderPageAsync(
            string validatedLocalPath,
            int pageNumber,
            double renderScale,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException(
                "Für diesen Generatorpfad darf keine PDF-Seite gerendert werden.");
    }

    private sealed class NullLocalLogService : ILocalLogService
    {
        public Task WriteAsync(LocalLogEntry entry, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<LocalLogEntry>> ReadRecentAsync(
            int maximumEntries,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LocalLogEntry>>([]);

        public Task ExportAsync(string targetPath, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task ClearAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
