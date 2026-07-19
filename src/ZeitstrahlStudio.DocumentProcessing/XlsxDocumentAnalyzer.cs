using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Shared;

namespace ZeitstrahlStudio.DocumentProcessing;

/// <summary>Extrahiert sichtbare Zellwerte und Kerneigenschaften aus XLSX-Dateien.</summary>
public sealed class XlsxDocumentAnalyzer : IDocumentAnalyzer
{
    private const string MediaType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

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
            var sharedStrings = await ReadSharedStringsAsync(archive, cancellationToken).ConfigureAwait(false);
            var worksheetEntries = archive.Entries
                .Where(entry =>
                    entry.FullName.StartsWith("xl/worksheets/", StringComparison.Ordinal) &&
                    entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                .OrderBy(entry => entry.FullName, StringComparer.Ordinal)
                .ToArray();
            if (worksheetEntries.Length == 0)
            {
                throw new InvalidDataException("Das XLSX-Dokument enthält kein Arbeitsblatt.");
            }

            var text = new StringBuilder();
            foreach (var worksheet in worksheetEntries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (text.Length > 0)
                {
                    OpenXmlAnalyzerSupport.AppendLimited(text, Environment.NewLine);
                }

                OpenXmlAnalyzerSupport.AppendLimited(
                    text,
                    $"[{Path.GetFileNameWithoutExtension(worksheet.Name)}]{Environment.NewLine}");
                await ReadWorksheetAsync(
                    worksheet,
                    sharedStrings,
                    text,
                    cancellationToken).ConfigureAwait(false);
            }

            var extractedText = text.ToString().Trim();
            var metadata = await OpenXmlAnalyzerSupport
                .ReadCorePropertiesAsync(archive, cancellationToken)
                .ConfigureAwait(false);
            return OperationResult<DocumentAnalysisResult>.Success(new DocumentAnalysisResult(
                MediaType,
                OpenXmlAnalyzerSupport.GetTitle(metadata, localFilePath),
                extractedText,
                Domain.TextExtractionMethod.OfficeDocument,
                metadata,
                OpenXmlAnalyzerSupport.ExtractDateSuggestions(extractedText),
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

    private static async Task<IReadOnlyList<string>> ReadSharedStringsAsync(
        ZipArchive archive,
        CancellationToken cancellationToken)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        var strings = new List<string>();
        StringBuilder? current = null;
        await using var stream = entry.Open();
        using var reader = OpenXmlAnalyzerSupport.CreateReader(stream);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "si")
            {
                current = new StringBuilder();
            }
            else if (reader.NodeType == XmlNodeType.Element &&
                     reader.LocalName == "t" &&
                     current is not null)
            {
                var value = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                OpenXmlAnalyzerSupport.AppendLimited(
                    current,
                    value);
                if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "si")
                {
                    strings.Add(current.ToString());
                    current = null;
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement &&
                     reader.LocalName == "si" &&
                     current is not null)
            {
                strings.Add(current.ToString());
                current = null;
            }
        }

        return strings;
    }

    private static async Task ReadWorksheetAsync(
        ZipArchiveEntry worksheet,
        IReadOnlyList<string> sharedStrings,
        StringBuilder output,
        CancellationToken cancellationToken)
    {
        string? cellType = null;
        await using var stream = worksheet.Open();
        using var reader = OpenXmlAnalyzerSupport.CreateReader(stream);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "c")
            {
                cellType = reader.GetAttribute("t");
            }
            else if (reader.NodeType == XmlNodeType.Element && reader.LocalName is "v" or "t")
            {
                var value = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                var displayed = ResolveCellValue(cellType, value, sharedStrings);
                OpenXmlAnalyzerSupport.AppendLimited(output, displayed);
                OpenXmlAnalyzerSupport.AppendLimited(output, "	");
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "row")
            {
                OpenXmlAnalyzerSupport.AppendLimited(output, Environment.NewLine);
            }
        }
    }

    private static string ResolveCellValue(
        string? cellType,
        string? value,
        IReadOnlyList<string> sharedStrings)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (cellType == "s" &&
            int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var index) &&
            index >= 0 &&
            index < sharedStrings.Count)
        {
            return sharedStrings[index];
        }

        return value;
    }
}
