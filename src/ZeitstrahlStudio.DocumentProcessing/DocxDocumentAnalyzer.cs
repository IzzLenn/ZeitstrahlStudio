using System.IO.Compression;
using System.Text;
using System.Xml;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Shared;

namespace ZeitstrahlStudio.DocumentProcessing;

/// <summary>Extrahiert DOCX-Text und Kerneigenschaften ohne Office-Installation.</summary>
public sealed class DocxDocumentAnalyzer : IDocumentAnalyzer
{
    private const string MediaType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public bool CanAnalyze(string mediaType) =>
        string.Equals(mediaType, MediaType, StringComparison.OrdinalIgnoreCase);

    public async Task<OperationResult<DocumentAnalysisResult>> AnalyzeAsync(
        string localFilePath,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var source = OpenXmlAnalyzerSupport.OpenSource(localFilePath);
            using var archive = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: false);
            OpenXmlAnalyzerSupport.ValidateArchive(archive);
            var documentEntry = archive.GetEntry("word/document.xml")
                ?? throw new InvalidDataException("Das DOCX-Dokument enthält keinen Haupttext.");
            var text = await ReadDocumentTextAsync(documentEntry, cancellationToken).ConfigureAwait(false);
            var metadata = await OpenXmlAnalyzerSupport
                .ReadCorePropertiesAsync(archive, cancellationToken)
                .ConfigureAwait(false);
            return OperationResult<DocumentAnalysisResult>.Success(new DocumentAnalysisResult(
                MediaType,
                OpenXmlAnalyzerSupport.GetTitle(metadata, localFilePath),
                text,
                Domain.TextExtractionMethod.OfficeDocument,
                metadata,
                OpenXmlAnalyzerSupport.ExtractDateSuggestions(text),
                ThumbnailRelativePath: null,
                PageCount: null));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException
                or InvalidDataException
                or XmlException
                or ArgumentException)
        {
            return OpenXmlAnalyzerSupport.Failure<DocumentAnalysisResult>(
                Path.GetFileName(localFilePath),
                exception);
        }
    }

    private static async Task<string> ReadDocumentTextAsync(
        ZipArchiveEntry documentEntry,
        CancellationToken cancellationToken)
    {
        var text = new StringBuilder();
        await using var stream = documentEntry.Open();
        using var reader = OpenXmlAnalyzerSupport.CreateReader(stream);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "t")
            {
                OpenXmlAnalyzerSupport.AppendLimited(
                    text,
                    await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
            }
            else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "tab")
            {
                OpenXmlAnalyzerSupport.AppendLimited(text, "	");
            }
            else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "br")
            {
                OpenXmlAnalyzerSupport.AppendLimited(text, Environment.NewLine);
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "p")
            {
                OpenXmlAnalyzerSupport.AppendLimited(text, Environment.NewLine);
            }
        }

        return text.ToString().Trim();
    }
}
