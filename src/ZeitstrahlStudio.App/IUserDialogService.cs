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
    string? RequestOpenProjectPath();
    string? RequestSaveProjectPath(string suggestedProjectName);
    SaveChangesDecision AskSaveChanges(string projectName);
    bool ConfirmDiscardRecovery(string projectName);
    bool ConfirmDeleteEvent(string eventTitle);
    void ShowError(string message, string? technicalDetails = null);
}
