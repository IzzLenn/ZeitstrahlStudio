using Microsoft.Win32;
using System.IO;
using System.Windows;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>WPF-Implementierung der interaktiven Dialoge.</summary>
public sealed class WpfUserDialogService : IUserDialogService
{
    public string? RequestProjectName()
    {
        var dialog = new NewProjectDialog
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return dialog.ShowDialog() == true ? dialog.ProjectName : null;
    }

    public EventEditRequest? RequestEvent(TimelineEvent? timelineEvent)
    {
        var dialog = new EventEditorDialog(timelineEvent)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return dialog.ShowDialog() == true ? dialog.Result : null;
    }

    public string? RequestOpenProjectPath()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Zeitstrahlprojekt öffnen",
            Filter = "Zeitstrahl-Studio-Projekt (*.zeitprojekt)|*.zeitprojekt",
            CheckFileExists = true,
            Multiselect = false,
        };
        return dialog.ShowDialog(System.Windows.Application.Current.MainWindow) == true
            ? dialog.FileName
            : null;
    }

    public string? RequestSaveProjectPath(string suggestedProjectName)
    {
        var safeName = string.Concat(suggestedProjectName.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
        var dialog = new SaveFileDialog
        {
            Title = "Zeitstrahlprojekt speichern",
            Filter = "Zeitstrahl-Studio-Projekt (*.zeitprojekt)|*.zeitprojekt",
            DefaultExt = ".zeitprojekt",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = string.IsNullOrWhiteSpace(safeName) ? "Neues Projekt" : safeName,
        };
        return dialog.ShowDialog(System.Windows.Application.Current.MainWindow) == true
            ? dialog.FileName
            : null;
    }

    public SaveChangesDecision AskSaveChanges(string projectName)
    {
        var result = MessageBox.Show(
            System.Windows.Application.Current.MainWindow,
            $"Möchten Sie die Änderungen an „{projectName}“ speichern?",
            "Projekt schließen",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question,
            MessageBoxResult.Yes);
        return result switch
        {
            MessageBoxResult.Yes => SaveChangesDecision.Save,
            MessageBoxResult.No => SaveChangesDecision.Discard,
            _ => SaveChangesDecision.Cancel,
        };
    }

    public bool ConfirmDiscardRecovery(string projectName) => MessageBox.Show(
        System.Windows.Application.Current.MainWindow,
        $"Soll die wiederherstellbare Arbeitskopie von „{projectName}“ endgültig verworfen werden?",
        "Arbeitskopie verwerfen",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning,
        MessageBoxResult.No) == MessageBoxResult.Yes;

    public bool ConfirmDeleteEvent(string eventTitle) => MessageBox.Show(
        System.Windows.Application.Current.MainWindow,
        $"Soll das Ereignis „{eventTitle}“ gelöscht werden?",
        "Ereignis löschen",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning,
        MessageBoxResult.No) == MessageBoxResult.Yes;

    public void ShowAuditLog(IReadOnlyList<AuditEntry> entries)
    {
        var dialog = new AuditLogDialog(entries)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        dialog.ShowDialog();
    }

    public void ShowError(string message, string? technicalDetails = null)
    {
        var text = string.IsNullOrWhiteSpace(technicalDetails)
            ? message
            : $"{message}\n\nTechnische Details:\n{technicalDetails}";
        MessageBox.Show(
            System.Windows.Application.Current.MainWindow,
            text,
            "Zeitstrahl Studio",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
