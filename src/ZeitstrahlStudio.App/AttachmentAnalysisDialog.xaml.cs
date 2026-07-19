using System.Windows;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Zeigt ein gespeichertes lokales Analyseergebnis schreibgeschützt an.</summary>
public partial class AttachmentAnalysisDialog : Window
{
    public AttachmentAnalysisDialog(Attachment attachment, DocumentAnalysisResult? result)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        InitializeComponent();
        DataContext = new AttachmentAnalysisDialogModel(
            attachment.OriginalFileName,
            attachment.MediaType,
            GetStateText(attachment.State, result is not null),
            result?.Title ?? string.Empty,
            result?.ExtractedText ??
                "Für diesen Anhang liegt noch kein extrahierter Text vor.",
            result?.DateSuggestions ?? [],
            result?.Metadata
                .OrderBy(item => item.Key, StringComparer.CurrentCultureIgnoreCase)
                .ToArray() ?? []);
    }

    private static string GetStateText(AttachmentState state, bool hasResult)
    {
        if (hasResult)
        {
            return "Status: Analyse bereit";
        }

        return state switch
        {
            AttachmentState.Imported => "Status: importiert, noch nicht analysiert",
            AttachmentState.Processing => "Status: Analyse läuft",
            AttachmentState.Ready => "Status: bereit, aber Ergebnis nicht lesbar",
            AttachmentState.Warning => "Status: Analyse mit Warnung",
            AttachmentState.Failed => "Status: Analyse fehlgeschlagen",
            _ => "Status: unbekannt",
        };
    }

    private sealed record AttachmentAnalysisDialogModel(
        string FileName,
        string MediaType,
        string StatusText,
        string DocumentTitle,
        string ExtractedText,
        IReadOnlyList<string> DateSuggestions,
        IReadOnlyList<KeyValuePair<string, string>> Metadata);
}
