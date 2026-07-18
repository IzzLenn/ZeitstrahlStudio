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

/// <summary>Analysiert Dokumente lokal und erzeugt nur übernehmbare Vorschläge.</summary>
public interface IDocumentAnalyzer
{
    bool CanAnalyze(string mediaType);
    Task<OperationResult<DocumentAnalysisResult>> AnalyzeAsync(
        string localFilePath,
        string workingDirectory,
        CancellationToken cancellationToken);
}

/// <summary>Durchsucht Projektdaten und extrahierte Dokumenttexte.</summary>
public interface IProjectSearchService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        Guid projectId,
        SearchCriteria criteria,
        CancellationToken cancellationToken);
}

/// <summary>Erzeugt eine Vorschau und eine druckoptimierte PDF-Datei.</summary>
public interface IPdfExportService
{
    Task<ExportPreview> CreatePreviewAsync(
        TimelineProject project,
        PdfExportOptions options,
        CancellationToken cancellationToken);

    Task ExportAsync(
        TimelineProject project,
        PdfExportOptions options,
        string targetPath,
        CancellationToken cancellationToken);
}

/// <summary>Erzeugt eine einzelne vollständig offlinefähige HTML-Datei.</summary>
public interface IHtmlExportService
{
    Task ExportAsync(
        TimelineProject project,
        HtmlExportOptions options,
        string workingDirectory,
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
    Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEntry>> ReadAsync(Guid projectId, CancellationToken cancellationToken);
}
