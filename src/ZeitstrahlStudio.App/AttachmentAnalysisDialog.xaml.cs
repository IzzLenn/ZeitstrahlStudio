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
            GetStateText(attachment.State, result),
            GetExtractionMethodText(result?.ExtractionMethod),
            result?.Title ?? string.Empty,
            result?.ExtractedText ??
                "Für diesen Anhang liegt noch kein extrahierter Text vor.",
            result?.DateSuggestions ?? [],
            result?.Metadata
                .OrderBy(item => item.Key, StringComparer.CurrentCultureIgnoreCase)
                .ToArray() ?? []);
    }

    private static string GetStateText(AttachmentState state, DocumentAnalysisResult? result)
    {
        if (result?.ExtractionMethod is TextExtractionMethod.Ocr or TextExtractionMethod.EmbeddedTextAndOcr)
        {
            return "Status: Analyse bereit – OCR-Ergebnisse können Erkennungsfehler enthalten";
        }

        if (result is not null)
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

    private static string GetExtractionMethodText(TextExtractionMethod? method) => method switch
    {
        TextExtractionMethod.EmbeddedText => "Textherkunft: eingebetteter PDF-Text",
        TextExtractionMethod.OfficeDocument => "Textherkunft: Dokumentinhalt",
        TextExtractionMethod.Ocr => "Textherkunft: lokale OCR (potenziell fehlerhaft)",
        TextExtractionMethod.EmbeddedTextAndOcr =>
            "Textherkunft: eingebetteter PDF-Text und lokale OCR (potenziell fehlerhaft)",
        _ => string.Empty,
    };

    private sealed record AttachmentAnalysisDialogModel(
        string FileName,
        string MediaType,
        string StatusText,
        string ExtractionMethodText,
        string DocumentTitle,
        string ExtractedText,
        IReadOnlyList<string> DateSuggestions,
        IReadOnlyList<KeyValuePair<string, string>> Metadata);
}
