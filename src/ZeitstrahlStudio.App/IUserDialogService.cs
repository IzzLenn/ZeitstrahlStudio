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
    string? RequestOpenProjectPath();
    string? RequestSaveProjectPath(string suggestedProjectName);
    SaveChangesDecision AskSaveChanges(string projectName);
    bool ConfirmDiscardRecovery(string projectName);
    bool ConfirmDeleteEvent(string eventTitle);
    void ShowAuditLog(IReadOnlyList<AuditEntry> entries);
    void ShowAttachmentAnalysis(Attachment attachment, DocumentAnalysisResult? result);
    void ShowImagePreview(Attachment attachment, string validatedLocalPath);
    void ShowError(string message, string? technicalDetails = null);
}
