using System.IO.Compression;
using ZeitstrahlStudio.DocumentProcessing;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class OpenXmlDocumentAnalyzerTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "ZeitstrahlStudio.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task DocxAnalyzer_ExtractsParagraphsMetadataAndDates()
    {
        var path = Path.Combine(CreateDirectory(), "beispiel.docx");
        using (var file = File.Create(path))
        using (var archive = new ZipArchive(file, ZipArchiveMode.Create))
        {
            WriteEntry(
                archive,
                "word/document.xml",
                """
                <?xml version="1.0" encoding="utf-8"?>
                <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                  <w:body>
                    <w:p><w:r><w:t>Projektstart 19.07.2026</w:t></w:r></w:p>
                    <w:p><w:r><w:t>Zweite Zeile</w:t></w:r></w:p>
                  </w:body>
                </w:document>
                """);
            WriteEntry(
                archive,
                "docProps/core.xml",
                """
                <?xml version="1.0" encoding="utf-8"?>
                <cp:coreProperties
                    xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties"
                    xmlns:dc="http://purl.org/dc/elements/1.1/">
                  <dc:title>Lokale Chronik</dc:title>
                  <dc:creator>Testautor</dc:creator>
                </cp:coreProperties>
                """);
        }

        var analyzer = new DocxDocumentAnalyzer();
        var result = await analyzer.AnalyzeAsync(path, directory, progress: null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TextExtractionMethod.OfficeDocument, result.Value!.ExtractionMethod);
        Assert.Equal("Lokale Chronik", result.Value.Title);
        Assert.Contains("Projektstart 19.07.2026", result.Value.ExtractedText);
        Assert.Contains("Zweite Zeile", result.Value.ExtractedText);
        Assert.Contains("19.07.2026", result.Value.DateSuggestions);
        Assert.Equal("Testautor", result.Value.Metadata["creator"]);
    }

    [Fact]
    public async Task XlsxAnalyzer_ResolvesSharedInlineAndNumericValues()
    {
        var path = Path.Combine(CreateDirectory(), "tabelle.xlsx");
        using (var file = File.Create(path))
        using (var archive = new ZipArchive(file, ZipArchiveMode.Create))
        {
            WriteEntry(
                archive,
                "xl/sharedStrings.xml",
                """
                <?xml version="1.0" encoding="utf-8"?>
                <sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <si><t>Meilenstein</t></si>
                  <si><t>2026-07-19</t></si>
                </sst>
                """);
            WriteEntry(
                archive,
                "xl/worksheets/sheet1.xml",
                """
                <?xml version="1.0" encoding="utf-8"?>
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <sheetData>
                    <row r="1">
                      <c r="A1" t="s"><v>0</v></c>
                      <c r="B1" t="s"><v>1</v></c>
                      <c r="C1"><v>42</v></c>
                    </row>
                    <row r="2"><c r="A2" t="inlineStr"><is><t>Notiz</t></is></c></row>
                  </sheetData>
                </worksheet>
                """);
        }

        var analyzer = new XlsxDocumentAnalyzer();
        var result = await analyzer.AnalyzeAsync(path, directory, progress: null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("Meilenstein", result.Value!.ExtractedText);
        Assert.Contains("2026-07-19", result.Value.ExtractedText);
        Assert.Contains("42", result.Value.ExtractedText);
        Assert.Contains("Notiz", result.Value.ExtractedText);
        Assert.Contains("2026-07-19", result.Value.DateSuggestions);
    }

    [Fact]
    public async Task Analyzer_ReturnsExpectedFailureForInvalidArchive()
    {
        var path = Path.Combine(CreateDirectory(), "defekt.docx");
        await File.WriteAllTextAsync(path, "kein ZIP");
        var analyzer = new DocxDocumentAnalyzer();

        var result = await analyzer.AnalyzeAsync(path, directory, progress: null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("DocumentAnalysisFailed", result.Error!.Code);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private string CreateDirectory()
    {
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }
}
