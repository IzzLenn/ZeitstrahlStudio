using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Shared;

namespace ZeitstrahlStudio.Application;

/// <summary>Geöffnetes Projekt mit ausschließlich lokalen Arbeits- und Archivpfaden.</summary>
public sealed record ProjectWorkspace(
    TimelineProject Project,
    string WorkingDirectory,
    string? ArchivePath,
    bool HasUnsavedChanges);

/// <summary>Fortschritt eines längeren Dateivorgangs.</summary>
public sealed record FileOperationProgress(
    string CurrentItem,
    int CompletedItems,
    int TotalItems,
    int SuccessfulItems,
    int FailedItems);

/// <summary>Feingranularer Fortschritt innerhalb einer einzelnen Dokumentanalyse.</summary>
public sealed record DocumentAnalysisProgress(
    string CurrentStep,
    int CompletedSteps,
    int TotalSteps);

/// <summary>Ergebnis einer Dokumentenanalyse.</summary>
public sealed record DocumentAnalysisResult(
    string MediaType,
    string? Title,
    string ExtractedText,
    TextExtractionMethod ExtractionMethod,
    IReadOnlyDictionary<string, string> Metadata,
    IReadOnlyList<string> DateSuggestions,
    string? ThumbnailRelativePath,
    int? PageCount);

/// <summary>Ergebnis einer einzelnen Anhangsanalyse innerhalb eines Stapels.</summary>
public sealed record AttachmentAnalysisOutcome(
    Attachment Attachment,
    OperationResult<DocumentAnalysisResult> Result);

/// <summary>Kombinierbare Such- und Filterkriterien.</summary>
public sealed record SearchCriteria(
    string? Query = null,
    DateOnly? From = null,
    DateOnly? Until = null,
    DatePrecision? Precision = null,
    bool? HasDeadline = null,
    DeadlineStatus? DeadlineStatus = null,
    EventPriority? Priority = null,
    string? ColorHex = null,
    string? Tag = null,
    string? MediaType = null,
    bool? HasAttachment = null,
    bool? HasPdf = null);

/// <summary>Ein Suchtreffer mit nachvollziehbaren Fundstellen.</summary>
public sealed record SearchResult(
    Guid EventId,
    string EventTitle,
    EventDate Date,
    double Relevance,
    IReadOnlyList<string> Highlights);

/// <summary>Parameter des druckoptimierten PDF-Exports.</summary>
public sealed record PdfExportOptions(
    string PaperSize,
    bool Landscape,
    double WidthMillimeters,
    double HeightMillimeters,
    double FontSize,
    DateOnly? RangeStart,
    DateOnly? RangeEnd,
    bool IncludeOverlappingRanges,
    bool SingleLargePage,
    bool IncludeNotes);

/// <summary>Parameter des eigenständigen HTML-Exports.</summary>
public sealed record HtmlExportOptions(
    TimelineOrientation InitialOrientation,
    bool IncludeThumbnails,
    bool IncludeNotes,
    bool ShowSnapshotBanner = true,
    bool IncludeDocumentCopies = false);

/// <summary>Validiertes Ergebnis eines Projektimports.</summary>
public sealed record ProjectImportResult(
    ProjectWorkspace Workspace,
    int ValidatedFiles,
    IReadOnlyList<string> Warnings);

/// <summary>Informationen zu einer erzeugten Exportvorschau.</summary>
public sealed record ExportPreview(
    int PageCount,
    double PageWidthMillimeters,
    double PageHeightMillimeters,
    IReadOnlyList<string> Warnings);

/// <summary>Gerenderte lokale PDF-Seite für die integrierte Vorschau.</summary>
public sealed record PdfPagePreview(
    int PageNumber,
    int PageCount,
    int PixelWidth,
    int PixelHeight,
    double EffectiveRenderScale,
    byte[] PngData);

/// <summary>Kleine lokal erzeugte Dokumentvorschau für eine sichtbare Zeitstrahlkarte.</summary>
public sealed record TimelineThumbnail(
    Guid AttachmentId,
    int PixelWidth,
    int PixelHeight,
    string CacheRelativePath,
    byte[] EncodedImageData);

/// <summary>Ein zuletzt verwendetes lokales Projektarchiv.</summary>
public sealed record RecentProject(
    string ProjectName,
    string ArchivePath,
    DateTimeOffset LastOpenedAtUtc,
    bool FileExists);

/// <summary>Eine nach einem unsauberen Programmende wiederherstellbare Arbeitskopie.</summary>
public sealed record RecoveryCandidate(
    Guid ProjectId,
    string ProjectName,
    string WorkingDirectory,
    string? ArchivePath,
    DateTimeOffset LastUpdatedAtUtc);

/// <summary>Statusmeldung eines automatischen Speichervorgangs.</summary>
public sealed record AutosaveStatus(
    DateTimeOffset TimestampUtc,
    bool Succeeded,
    string Message,
    ApplicationError? Error = null);

/// <summary>Bearbeitbarer Webseitenverweis eines Ereignisses.</summary>
public sealed record WebLinkInput(
    Guid? Id,
    string Address,
    string? Label);

/// <summary>Vollständige validierbare Eingabe für ein Ereignis.</summary>
public sealed record EventEditRequest(
    EventDate Date,
    string Title,
    string? InfoText,
    string? Description,
    Deadline? Deadline,
    EventPriority Priority,
    string ColorHex,
    string? Source,
    string? Notes,
    EventStatus Status,
    IReadOnlyList<string> Tags,
    IReadOnlyList<WebLinkInput> WebLinks);

/// <summary>Schweregrad eines ausschließlich lokalen technischen Protokolleintrags.</summary>
public enum LocalLogLevel
{
    Debug,
    Information,
    Warning,
    Error,
}

/// <summary>Strukturierter lokaler Protokolleintrag ohne Dokumentinhalt.</summary>
public sealed record LocalLogEntry(
    DateTimeOffset TimestampUtc,
    LocalLogLevel Level,
    string Category,
    string EventName,
    string Message,
    string? TechnicalDetails = null);
