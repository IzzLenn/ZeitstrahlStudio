namespace ZeitstrahlStudio.App;

using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

/// <summary>Entscheidung beim Schließen eines geänderten Projekts.</summary>
public enum SaveChangesDecision
{
    Save,
    Discard,
    Cancel,
}

/// <summary>Vom Benutzer bestätigte Einstellungen und Zieldatei eines HTML-Exports.</summary>
public sealed record HtmlExportRequest(HtmlExportOptions Options, string TargetPath);

/// <summary>Ergebnis der modalen Sicherungsverwaltung.</summary>
public sealed record BackupManagerResult(
    ProjectWorkspace Workspace,
    bool WasRestored,
    bool SettingsChanged);

/// <summary>Kapselt ausschließlich WPF-spezifische Datei- und Benutzerdialoge.</summary>
public interface IUserDialogService
{
    string? RequestProjectName();
    EventEditRequest? RequestEvent(TimelineEvent? timelineEvent);
    IReadOnlyList<string> RequestAttachmentPaths();
    Attachment? RequestAttachmentToRemove(TimelineEvent timelineEvent);
    Attachment? RequestAttachmentForAnalysis(TimelineEvent timelineEvent);
    Attachment? RequestAttachmentToOpen(TimelineEvent timelineEvent);
    Attachment? RequestImageForPreview(IReadOnlyCollection<Attachment> attachments);
    Attachment? RequestPdfForPreview(IReadOnlyCollection<Attachment> attachments);
    string? RequestOpenProjectPath();
    string? RequestSaveProjectPath(string suggestedProjectName);
    SaveChangesDecision AskSaveChanges(string projectName);
    bool ConfirmDiscardRecovery(string projectName);
    bool ConfirmDeleteEvent(string eventTitle);
    void ShowAuditLog(IReadOnlyList<AuditEntry> entries);
    void ShowAttachmentAnalysis(Attachment attachment, DocumentAnalysisResult? result);
    void ShowImagePreview(Attachment attachment, string validatedLocalPath);
    Task ShowPdfPreviewAsync(
        Attachment attachment,
        string validatedLocalPath,
        Func<CancellationToken, Task> openExternallyAsync,
        CancellationToken cancellationToken);
    Task<string?> ShowPdfExportAsync(
        ProjectWorkspace workspace,
        CancellationToken cancellationToken);
    Task<BackupManagerResult> ShowBackupManagerAsync(
        ProjectWorkspace workspace,
        CancellationToken cancellationToken);
    HtmlExportRequest? RequestHtmlExport(TimelineProject project);
    void ShowError(string message, string? technicalDetails = null);
}
