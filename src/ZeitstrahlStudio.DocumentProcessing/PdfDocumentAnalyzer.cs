using System.Runtime.InteropServices;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.Exceptions;
using UglyToad.PdfPig.Fonts;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Shared;

namespace ZeitstrahlStudio.DocumentProcessing;

/// <summary>Extrahiert PDF-Text und ergänzt bildbasierte Seiten durch vollständig lokale OCR.</summary>
public sealed class PdfDocumentAnalyzer : IDocumentAnalyzer
{
    private const string MediaType = "application/pdf";
    private const int MaximumPages = 100_000;
    private const int MaximumOcrPages = 250;
    private const int MaximumExtractedCharacters = 10_000_000;
    private const double OcrRenderScale = 3d;
    private readonly ILocalOcrService ocrService;
    private readonly IPdfPreviewService pdfPreviewService;

    public PdfDocumentAnalyzer(
        ILocalOcrService ocrService,
        IPdfPreviewService pdfPreviewService)
    {
        this.ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
        this.pdfPreviewService = pdfPreviewService ??
            throw new ArgumentNullException(nameof(pdfPreviewService));
    }

    public bool CanAnalyze(string mediaType) =>
        string.Equals(mediaType, MediaType, StringComparison.OrdinalIgnoreCase);

    public async Task<OperationResult<DocumentAnalysisResult>> AnalyzeAsync(
        string localFilePath,
        string workingDirectory,
        IProgress<DocumentAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = Path.GetFullPath(localFilePath);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Die PDF-Projektkopie wurde nicht gefunden.", path);
            }

            var snapshot = await Task.Run(
                () => ExtractEmbeddedText(path, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            var ocrPageNumbers = snapshot.PageTexts
                .Select((text, index) => (text, PageNumber: index + 1))
                .Where(item => string.IsNullOrWhiteSpace(item.text))
                .Select(item => item.PageNumber)
                .ToArray();
            if (ocrPageNumbers.Length > MaximumOcrPages)
            {
                throw new InvalidDataException(
                    $"Das PDF benötigt OCR für {ocrPageNumbers.Length} Seiten. " +
                    $"Das Sicherheitslimit liegt bei {MaximumOcrPages} Seiten pro Analyse.");
            }

            var pageTexts = snapshot.PageTexts.ToArray();
            string? ocrLanguage = null;
            progress?.Report(new DocumentAnalysisProgress(
                ocrPageNumbers.Length == 0
                    ? "Eingebetteter PDF-Text wurde gelesen"
                    : $"{ocrPageNumbers.Length} bildbasierte PDF-Seite(n) erkannt",
                ocrPageNumbers.Length == 0 ? 1 : 0,
                Math.Max(1, ocrPageNumbers.Length)));
            for (var index = 0; index < ocrPageNumbers.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var pageNumber = ocrPageNumbers[index];
                progress?.Report(new DocumentAnalysisProgress(
                    $"OCR für PDF-Seite {pageNumber} von {snapshot.PageTexts.Count}",
                    index,
                    ocrPageNumbers.Length));
                var preview = await pdfPreviewService.RenderPageAsync(
                    path,
                    pageNumber,
                    OcrRenderScale,
                    cancellationToken).ConfigureAwait(false);
                var ocr = await ocrService.RecognizePngAsync(
                    preview.PngData,
                    cancellationToken).ConfigureAwait(false);
                pageTexts[pageNumber - 1] = ocr.Text;
                ocrLanguage ??= ocr.LanguageTag;
                progress?.Report(new DocumentAnalysisProgress(
                    $"OCR für PDF-Seite {pageNumber} von {snapshot.PageTexts.Count}",
                    index + 1,
                    ocrPageNumbers.Length));
            }

            var extractedText = JoinPageTexts(pageTexts);
            var metadata = new Dictionary<string, string>(
                snapshot.Metadata,
                StringComparer.OrdinalIgnoreCase);
            var extractionMethod = TextExtractionMethod.EmbeddedText;
            if (ocrPageNumbers.Length > 0)
            {
                extractionMethod = ocrPageNumbers.Length == snapshot.PageTexts.Count
                    ? TextExtractionMethod.Ocr
                    : TextExtractionMethod.EmbeddedTextAndOcr;
                metadata["ocrEngine"] = "Windows OCR (lokal)";
                metadata["ocrLanguage"] = ocrLanguage ?? "de-DE";
                metadata["ocrPages"] = string.Join(", ", ocrPageNumbers);
                metadata["ocrNotice"] =
                    "OCR-Ergebnisse sind Vorschläge und können Erkennungsfehler enthalten.";
            }

            var suggestionSource = string.Join(
                Environment.NewLine,
                metadata.Values.Prepend(extractedText));
            return OperationResult<DocumentAnalysisResult>.Success(new DocumentAnalysisResult(
                MediaType,
                snapshot.Title,
                extractedText,
                extractionMethod,
                metadata,
                OpenXmlAnalyzerSupport.ExtractDateSuggestions(suggestionSource),
                ThumbnailRelativePath: null,
                PageCount: snapshot.PageTexts.Count));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or
                UnauthorizedAccessException or
                InvalidDataException or
                ArgumentException or
                InvalidOperationException or
                NotSupportedException or
                COMException or
                PdfDocumentFormatException or
                PdfDocumentEncryptedException or
                InvalidFontFormatException or
                CorruptCompressedDataException)
        {
            return OpenXmlAnalyzerSupport.Failure<DocumentAnalysisResult>(
                Path.GetFileName(localFilePath),
                exception);
        }
    }

    private static PdfExtractionSnapshot ExtractEmbeddedText(
        string path,
        CancellationToken cancellationToken)
    {
        using var document = PdfDocument.Open(
            path,
            new ParsingOptions
            {
                UseLenientParsing = true,
                SkipMissingFonts = true,
                MaxStackDepth = 64,
                UseActualText = true,
            });
        if (document.NumberOfPages < 1 || document.NumberOfPages > MaximumPages)
        {
            throw new InvalidDataException(
                $"Das PDF enthält eine unzulässige Seitenzahl ({document.NumberOfPages}).");
        }

        var pageTexts = new string[document.NumberOfPages];
        for (var pageNumber = 1; pageNumber <= document.NumberOfPages; pageNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            pageTexts[pageNumber - 1] =
                ContentOrderTextExtractor.GetText(document.GetPage(pageNumber)).Trim();
        }

        var title = string.IsNullOrWhiteSpace(document.Information.Title)
            ? Path.GetFileNameWithoutExtension(path)
            : document.Information.Title;
        return new PdfExtractionSnapshot(
            title,
            pageTexts,
            GetMetadata(document));
    }

    private static string JoinPageTexts(IEnumerable<string> pageTexts)
    {
        var text = new StringBuilder();
        foreach (var pageText in pageTexts)
        {
            if (string.IsNullOrWhiteSpace(pageText))
            {
                continue;
            }

            if (text.Length > 0)
            {
                AppendLimited(text, Environment.NewLine + Environment.NewLine);
            }

            AppendLimited(text, pageText.Trim());
        }

        return text.ToString();
    }

    private static IReadOnlyDictionary<string, string> GetMetadata(PdfDocument document)
    {
        var information = document.Information;
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AddMetadata(metadata, "title", information.Title);
        AddMetadata(metadata, "author", information.Author);
        AddMetadata(metadata, "subject", information.Subject);
        AddMetadata(metadata, "keywords", information.Keywords);
        AddMetadata(metadata, "creator", information.Creator);
        AddMetadata(metadata, "producer", information.Producer);
        AddMetadata(metadata, "created", information.CreationDate);
        AddMetadata(metadata, "modified", information.ModifiedDate);
        metadata["pdfVersion"] = document.Version.ToString();
        metadata["encrypted"] = document.IsEncrypted ? "true" : "false";
        return metadata;
    }

    private static void AddMetadata(
        IDictionary<string, string> metadata,
        string key,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            metadata[key] = value.Trim();
        }
    }

    private static void AppendLimited(StringBuilder text, string value)
    {
        if (text.Length + value.Length > MaximumExtractedCharacters)
        {
            throw new InvalidDataException(
                "Der extrahierte PDF-Text überschreitet das Sicherheitslimit.");
        }

        text.Append(value);
    }

    private sealed record PdfExtractionSnapshot(
        string Title,
        IReadOnlyList<string> PageTexts,
        IReadOnlyDictionary<string, string> Metadata);
}
