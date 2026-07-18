namespace ZeitstrahlStudio.App;

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
    string? RequestOpenProjectPath();
    string? RequestSaveProjectPath(string suggestedProjectName);
    SaveChangesDecision AskSaveChanges(string projectName);
    bool ConfirmDiscardRecovery(string projectName);
    void ShowError(string message, string? technicalDetails = null);
}
