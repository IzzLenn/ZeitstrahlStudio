using ZeitstrahlStudio.Domain;

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
    bool IncludeNotes);

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
