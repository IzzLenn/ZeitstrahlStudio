using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Shared;

namespace ZeitstrahlStudio.Application;

/// <summary>Persistiert das normalisierte Projektmodell transaktionssicher.</summary>
public interface IProjectRepository
{
    Task InitializeAsync(string databasePath, CancellationToken cancellationToken);
    Task SaveAsync(TimelineProject project, string databasePath, CancellationToken cancellationToken);
    Task<TimelineProject> LoadAsync(string databasePath, CancellationToken cancellationToken);
}

/// <summary>Verwaltet Arbeitsordner und atomare Speichervorgänge.</summary>
public interface IProjectWorkspaceService
{
    Task<ProjectWorkspace> CreateAsync(string projectName, string archivePath, CancellationToken cancellationToken);
    Task<ProjectWorkspace> OpenAsync(string archivePath, CancellationToken cancellationToken);
    Task<ProjectWorkspace> SaveAsync(
        ProjectWorkspace workspace,
        string? targetArchivePath,
        CancellationToken cancellationToken);
    Task<ProjectWorkspace> CheckpointAsync(
        ProjectWorkspace workspace,
        CancellationToken cancellationToken);
    Task<ProjectWorkspace> DuplicateAsync(ProjectWorkspace workspace, string targetArchivePath, CancellationToken cancellationToken);
    Task CloseAsync(ProjectWorkspace workspace, CancellationToken cancellationToken);
    Task DeleteArchiveAsync(string archivePath, bool deletionConfirmed, CancellationToken cancellationToken);
}

/// <summary>Erstellt und validiert das versionierte ZIP-basierte Projektformat.</summary>
public interface IProjectArchiveService
{
    Task ExportAsync(
        string workingDirectory,
        string targetArchivePath,
        IProgress<FileOperationProgress>? progress,
        CancellationToken cancellationToken);

    Task<OperationResult<ProjectImportResult>> ImportAsync(
        string archivePath,
        string targetWorkingDirectory,
        IProgress<FileOperationProgress>? progress,
        CancellationToken cancellationToken);
}

/// <summary>Importiert Originaldateien kollisionsfrei in ein Projekt.</summary>
public interface IAttachmentImportService
{
    Task<IReadOnlyList<OperationResult<Attachment>>> ImportAsync(
        Guid eventId,
        IReadOnlyCollection<string> sourcePaths,
        string workingDirectory,
        IProgress<FileOperationProgress>? progress,
        CancellationToken cancellationToken);
}

/// <summary>Validiert Projektkopien und öffnet sie auf ausdrücklichen Benutzerwunsch lokal.</summary>
public interface IAttachmentFileService
{
    Task<string> GetValidatedLocalPathAsync(
        ProjectWorkspace workspace,
        Attachment attachment,
        CancellationToken cancellationToken);
    Task OpenWithDefaultApplicationAsync(
        ProjectWorkspace workspace,
        Attachment attachment,
        CancellationToken cancellationToken);
}

/// <summary>Analysiert Dokumente lokal und erzeugt nur übernehmbare Vorschläge.</summary>
public interface IDocumentAnalyzer
{
    bool CanAnalyze(string mediaType);
    Task<OperationResult<DocumentAnalysisResult>> AnalyzeAsync(
        string localFilePath,
        string workingDirectory,
        IProgress<DocumentAnalysisProgress>? progress,
        CancellationToken cancellationToken);
}

/// <summary>Speichert und lädt lokale Dokumentanalyseergebnisse transaktionssicher.</summary>
public interface IAttachmentAnalysisStore
{
    Task SaveAsync(
        ProjectWorkspace workspace,
        Attachment attachment,
        DocumentAnalysisResult result,
        CancellationToken cancellationToken);
    Task<DocumentAnalysisResult?> LoadAsync(
        ProjectWorkspace workspace,
        Attachment attachment,
        CancellationToken cancellationToken);
}

/// <summary>Analysiert unterstützte Anhänge mit begrenzter Parallelität und speichert die Ergebnisse.</summary>
public interface IAttachmentAnalysisQueue
{
    Task<IReadOnlyList<AttachmentAnalysisOutcome>> AnalyzeAsync(
        ProjectWorkspace workspace,
        IReadOnlyCollection<Attachment> attachments,
        IProgress<FileOperationProgress>? progress,
        CancellationToken cancellationToken);
}

/// <summary>Rendert einzelne PDF-Seiten lokal und ressourcenbegrenzt für die Vorschau.</summary>
public interface IPdfPreviewService
{
    Task<PdfPagePreview> RenderPageAsync(
        string validatedLocalPath,
        int pageNumber,
        double renderScale,
        CancellationToken cancellationToken);
}

/// <summary>Durchsucht Projektdaten und extrahierte Dokumenttexte.</summary>
public interface IProjectSearchService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        ProjectWorkspace workspace,
        SearchCriteria criteria,
        CancellationToken cancellationToken);
}

/// <summary>Erzeugt eine Vorschau und eine druckoptimierte PDF-Datei.</summary>
public interface IPdfExportService
{
    Task<ExportPreview> CreatePreviewAsync(
        ProjectWorkspace workspace,
        PdfExportOptions options,
        CancellationToken cancellationToken);

    Task ExportAsync(
        ProjectWorkspace workspace,
        PdfExportOptions options,
        string targetPath,
        CancellationToken cancellationToken);
}

/// <summary>Erzeugt eine einzelne vollständig offlinefähige HTML-Datei.</summary>
public interface IHtmlExportService
{
    Task ExportAsync(
        ProjectWorkspace workspace,
        HtmlExportOptions options,
        string targetPath,
        CancellationToken cancellationToken);
}

/// <summary>Erstellt, rotiert und restauriert Projektsicherungen.</summary>
public interface IBackupService
{
    Task<BackupRecord> CreateAsync(ProjectWorkspace workspace, bool automatic, CancellationToken cancellationToken);
    Task<IReadOnlyList<BackupRecord>> ListAsync(Guid projectId, CancellationToken cancellationToken);
    Task<ProjectWorkspace> RestoreAsync(BackupRecord backup, CancellationToken cancellationToken);
    Task ApplyRetentionAsync(Guid projectId, ProjectSettings settings, CancellationToken cancellationToken);
}

/// <summary>Schreibt das lokale, projektbezogene Änderungsprotokoll.</summary>
public interface IAuditLogService
{
    Task WriteAsync(
        ProjectWorkspace workspace,
        AuditEntry entry,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEntry>> ReadAsync(
        ProjectWorkspace workspace,
        CancellationToken cancellationToken);
}

/// <summary>Persistiert die Liste zuletzt geöffneter Projektarchive lokal.</summary>
public interface IRecentProjectsService
{
    Task<IReadOnlyList<RecentProject>> GetAsync(CancellationToken cancellationToken);
    Task RecordOpenedAsync(ProjectWorkspace workspace, CancellationToken cancellationToken);
    Task RemoveAsync(string archivePath, CancellationToken cancellationToken);
    Task RemoveMissingAsync(CancellationToken cancellationToken);
}

/// <summary>Findet und verwaltet nach einem Absturz verbliebene Arbeitskopien.</summary>
public interface IProjectRecoveryService
{
    Task<IReadOnlyList<RecoveryCandidate>> FindAsync(CancellationToken cancellationToken);
    Task<ProjectWorkspace> RecoverAsync(RecoveryCandidate candidate, CancellationToken cancellationToken);
    Task DiscardAsync(RecoveryCandidate candidate, CancellationToken cancellationToken);
}

/// <summary>Serialisiert zeitgesteuerte Speichervorgänge und meldet Fehler, ohne die Schleife zu beenden.</summary>
public interface IProjectAutosaveService
{
    Task RunAsync(
        Func<ProjectWorkspace?> currentWorkspace,
        Action<ProjectWorkspace> workspaceUpdated,
        TimeSpan interval,
        IProgress<AutosaveStatus>? progress,
        CancellationToken cancellationToken);
}

/// <summary>Schreibt, liest, exportiert und löscht rotierende lokale technische Logs.</summary>
public interface ILocalLogService
{
    Task WriteAsync(LocalLogEntry entry, CancellationToken cancellationToken);
    Task<IReadOnlyList<LocalLogEntry>> ReadRecentAsync(int maximumEntries, CancellationToken cancellationToken);
    Task ExportAsync(string targetPath, CancellationToken cancellationToken);
    Task ClearAsync(CancellationToken cancellationToken);
}
