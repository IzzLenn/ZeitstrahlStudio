using Microsoft.Win32;
using System.IO;
using System.Windows;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>WPF-Implementierung der interaktiven Dialoge.</summary>
public sealed class WpfUserDialogService : IUserDialogService
{
    private readonly IPdfPreviewService pdfPreviewService;
    private readonly IPdfExportService pdfExportService;
    private readonly ILocalLogService logService;

    public WpfUserDialogService(
        IPdfPreviewService pdfPreviewService,
        IPdfExportService pdfExportService,
        ILocalLogService logService)
    {
        this.pdfPreviewService = pdfPreviewService;
        this.pdfExportService = pdfExportService;
        this.logService = logService;
    }

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

    public IReadOnlyList<string> RequestAttachmentPaths()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Anhänge zum Ereignis hinzufügen",
            Filter = "Unterstützte Dokumente|*.pdf;*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.bmp;*.docx;*.xlsx|Alle Dateien|*.*",
            CheckFileExists = true,
            Multiselect = true,
        };
        return dialog.ShowDialog(System.Windows.Application.Current.MainWindow) == true
            ? dialog.FileNames
            : [];
    }

    public Attachment? RequestAttachmentToRemove(TimelineEvent timelineEvent)
    {
        var dialog = new AttachmentSelectionDialog(
            timelineEvent.Attachments,
            "Anhang entfernen",
            "Anhangszuordnung entfernen",
            "Die Projektkopie bleibt für Undo erhalten.",
            "Entfernen",
            isDestructive: true)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return dialog.ShowDialog() == true ? dialog.Result : null;
    }

    public Attachment? RequestAttachmentForAnalysis(TimelineEvent timelineEvent)
    {
        var dialog = new AttachmentSelectionDialog(
            timelineEvent.Attachments,
            "Analyse anzeigen",
            "Dokumentanalyse anzeigen",
            "Wählen Sie einen Anhang aus, um Status, Text und Datumsfundstellen zu prüfen.",
            "Anzeigen",
            isDestructive: false)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return dialog.ShowDialog() == true ? dialog.Result : null;
    }

    public Attachment? RequestAttachmentToOpen(TimelineEvent timelineEvent)
    {
        var dialog = new AttachmentSelectionDialog(
            timelineEvent.Attachments,
            "Projektkopie öffnen",
            "Anhang im Standardprogramm öffnen",
            "Die zuvor geprüfte lokale Projektkopie wird an Windows übergeben.",
            "Öffnen",
            isDestructive: false)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return dialog.ShowDialog() == true ? dialog.Result : null;
    }

    public Attachment? RequestImageForPreview(IReadOnlyCollection<Attachment> attachments)
    {
        var dialog = new AttachmentSelectionDialog(
            attachments,
            "Bildvorschau",
            "Bild lokal anzeigen",
            "Wählen Sie eine geprüfte Bild-Projektkopie aus.",
            "Anzeigen",
            isDestructive: false)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return dialog.ShowDialog() == true ? dialog.Result : null;
    }

    public Attachment? RequestPdfForPreview(IReadOnlyCollection<Attachment> attachments)
    {
        var dialog = new AttachmentSelectionDialog(
            attachments,
            "PDF-Vorschau",
            "PDF lokal anzeigen",
            "Wählen Sie eine geprüfte PDF-Projektkopie aus.",
            "Anzeigen",
            isDestructive: false)
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

    public void ShowAttachmentAnalysis(Attachment attachment, DocumentAnalysisResult? result)
    {
        var dialog = new AttachmentAnalysisDialog(attachment, result)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        dialog.ShowDialog();
    }

    public void ShowImagePreview(Attachment attachment, string validatedLocalPath)
    {
        var dialog = new AttachmentImagePreviewDialog(attachment, validatedLocalPath)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        dialog.ShowDialog();
    }

    public async Task ShowPdfPreviewAsync(
        Attachment attachment,
        string validatedLocalPath,
        Func<CancellationToken, Task> openExternallyAsync,
        CancellationToken cancellationToken)
    {
        var viewModel = new PdfPreviewDialogViewModel(
            pdfPreviewService,
            logService,
            attachment,
            validatedLocalPath,
            openExternallyAsync);
        try
        {
            await viewModel.InitializeAsync(cancellationToken).ConfigureAwait(true);
            var dialog = new AttachmentPdfPreviewDialog(viewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow,
            };
            dialog.ShowDialog();
        }
        finally
        {
            viewModel.Dispose();
        }
    }

    public async Task<string?> ShowPdfExportAsync(
        ProjectWorkspace workspace,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        using var viewModel = new PdfExportDialogViewModel(
            pdfExportService,
            pdfPreviewService,
            logService,
            workspace,
            () => RequestPdfExportTargetPath(workspace.Project.Name));
        await viewModel.InitializeAsync(cancellationToken).ConfigureAwait(true);
        var dialog = new PdfExportDialog(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return dialog.ShowDialog() == true ? viewModel.ExportedTargetPath : null;
    }

    private static string? RequestPdfExportTargetPath(string projectName)
    {
        var safeName = string.Concat(projectName.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
        var dialog = new SaveFileDialog
        {
            Title = "Zeitstrahl als PDF speichern",
            Filter = "PDF-Datei (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = string.IsNullOrWhiteSpace(safeName) ? "Zeitstrahl" : safeName,
        };
        return dialog.ShowDialog(System.Windows.Application.Current.MainWindow) == true
            ? dialog.FileName
            : null;
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
