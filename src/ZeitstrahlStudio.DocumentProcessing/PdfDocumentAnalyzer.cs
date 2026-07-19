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

/// <summary>Extrahiert eingebetteten PDF-Text und Dokumentmetadaten vollständig lokal.</summary>
public sealed class PdfDocumentAnalyzer : IDocumentAnalyzer
{
    private const string MediaType = "application/pdf";
    private const int MaximumPages = 100_000;
    private const int MaximumExtractedCharacters = 10_000_000;

    public bool CanAnalyze(string mediaType) =>
        string.Equals(mediaType, MediaType, StringComparison.OrdinalIgnoreCase);

    public Task<OperationResult<DocumentAnalysisResult>> AnalyzeAsync(
        string localFilePath,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => Analyze(localFilePath, cancellationToken),
            cancellationToken);
    }

    private static OperationResult<DocumentAnalysisResult> Analyze(
        string localFilePath,
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

            var text = new StringBuilder();
            for (var pageNumber = 1; pageNumber <= document.NumberOfPages; pageNumber++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var pageText = ContentOrderTextExtractor.GetText(document.GetPage(pageNumber));
                AppendLimited(text, pageText);
                if (pageNumber < document.NumberOfPages && text.Length > 0)
                {
                    AppendLimited(text, Environment.NewLine + Environment.NewLine);
                }
            }

            var metadata = GetMetadata(document);
            var extractedText = text.ToString().Trim();
            var suggestionSource = string.Join(
                Environment.NewLine,
                metadata.Values.Prepend(extractedText));
            return OperationResult<DocumentAnalysisResult>.Success(new DocumentAnalysisResult(
                MediaType,
                string.IsNullOrWhiteSpace(document.Information.Title)
                    ? Path.GetFileNameWithoutExtension(path)
                    : document.Information.Title,
                extractedText,
                TextExtractionMethod.EmbeddedText,
                metadata,
                OpenXmlAnalyzerSupport.ExtractDateSuggestions(suggestionSource),
                ThumbnailRelativePath: null,
                PageCount: document.NumberOfPages));
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

    private static void AppendLimited(StringBuilder text, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (text.Length + value.Length > MaximumExtractedCharacters)
        {
            throw new InvalidDataException(
                "Der extrahierte PDF-Text überschreitet das Sicherheitslimit.");
        }

        text.Append(value);
    }
}
