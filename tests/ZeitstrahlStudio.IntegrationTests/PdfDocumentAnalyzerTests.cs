using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using ZeitstrahlStudio.DocumentProcessing;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class PdfDocumentAnalyzerTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "ZeitstrahlStudio.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task AnalyzeAsync_ExtractsTextMetadataDatesAndPageCount()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "chronik.pdf");
        using (var builder = new PdfDocumentBuilder())
        {
            builder.DocumentInformation.Title = "Lokale PDF-Chronik";
            builder.DocumentInformation.Author = "Testautor";
            var font = builder.AddStandard14Font(Standard14Font.Helvetica);
            var firstPage = builder.AddPage(PageSize.A4);
            firstPage.AddText(
                "Projektstart 19.07.2026",
                12,
                new PdfPoint(50, 750),
                font);
            var secondPage = builder.AddPage(PageSize.A4);
            secondPage.AddText(
                "Zweiter Meilenstein",
                12,
                new PdfPoint(50, 750),
                font);
            await File.WriteAllBytesAsync(path, builder.Build());
        }

        var analyzer = new PdfDocumentAnalyzer();
        var result = await analyzer.AnalyzeAsync(path, directory, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.TechnicalDetails);
        Assert.Equal(TextExtractionMethod.EmbeddedText, result.Value!.ExtractionMethod);
        Assert.Equal("Lokale PDF-Chronik", result.Value.Title);
        Assert.Equal("Testautor", result.Value.Metadata["author"]);
        Assert.Equal(2, result.Value.PageCount);
        Assert.Contains("Projektstart 19.07.2026", result.Value.ExtractedText);
        Assert.Contains("Zweiter Meilenstein", result.Value.ExtractedText);
        Assert.Contains("19.07.2026", result.Value.DateSuggestions);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsFailureForDamagedPdf()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "defekt.pdf");
        await File.WriteAllTextAsync(path, "%PDF-1.7 beschädigt");
        var analyzer = new PdfDocumentAnalyzer();

        var result = await analyzer.AnalyzeAsync(path, directory, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("DocumentAnalysisFailed", result.Error!.Code);
    }

    [Fact]
    public async Task AnalyzeAsync_PropagatesCancellation()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "abbruch.pdf");
        await File.WriteAllTextAsync(path, "%PDF-1.7");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var analyzer = new PdfDocumentAnalyzer();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            analyzer.AnalyzeAsync(path, directory, cancellation.Token));
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
