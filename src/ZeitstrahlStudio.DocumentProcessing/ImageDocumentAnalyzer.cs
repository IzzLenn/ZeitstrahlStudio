using System.Runtime.InteropServices;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;
using ZeitstrahlStudio.Shared;

namespace ZeitstrahlStudio.DocumentProcessing;

/// <summary>Erkennt Text in PNG-, JPEG-, TIFF- und BMP-Projektkopien vollständig lokal.</summary>
public sealed class ImageDocumentAnalyzer : IDocumentAnalyzer
{
    private static readonly IReadOnlyDictionary<string, string> MediaTypesByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".tif"] = "image/tiff",
            [".tiff"] = "image/tiff",
            [".bmp"] = "image/bmp",
        };

    private readonly ILocalOcrService ocrService;

    public ImageDocumentAnalyzer(ILocalOcrService ocrService)
    {
        this.ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
    }

    public bool CanAnalyze(string mediaType) =>
        MediaTypesByExtension.Values.Contains(mediaType, StringComparer.OrdinalIgnoreCase);

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
            if (!MediaTypesByExtension.TryGetValue(Path.GetExtension(path), out var mediaType))
            {
                throw new InvalidDataException("Das Bildformat wird von der lokalen OCR nicht unterstützt.");
            }

            var ocr = await ocrService.RecognizeFileAsync(
                path,
                progress,
                cancellationToken).ConfigureAwait(false);
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ocrEngine"] = "Windows OCR (lokal)",
                ["ocrLanguage"] = ocr.LanguageTag,
                ["ocrNotice"] =
                    "OCR-Ergebnisse sind Vorschläge und können Erkennungsfehler enthalten.",
                ["ocrPixelSize"] = $"{ocr.MaximumPixelWidth} × {ocr.MaximumPixelHeight}",
            };
            if (ocr.PageCount > 1)
            {
                metadata["imageFrames"] = ocr.PageCount.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            }

            return OperationResult<DocumentAnalysisResult>.Success(new DocumentAnalysisResult(
                mediaType,
                Path.GetFileNameWithoutExtension(path),
                ocr.Text,
                TextExtractionMethod.Ocr,
                metadata,
                OpenXmlAnalyzerSupport.ExtractDateSuggestions(ocr.Text),
                ThumbnailRelativePath: null,
                PageCount: ocr.PageCount));
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
                COMException)
        {
            return OperationResult<DocumentAnalysisResult>.Failure(new ApplicationError(
                "OcrFailed",
                $"Das Bild „{Path.GetFileName(localFilePath)}“ konnte nicht lokal per OCR analysiert werden.",
                exception.Message));
        }
    }
}
