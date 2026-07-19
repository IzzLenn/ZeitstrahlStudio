using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.DocumentProcessing;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class ImageDocumentAnalyzerTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "ZeitstrahlStudio.Tests",
        Guid.NewGuid().ToString("N"));
    private readonly WindowsLocalOcrService ocrService = new();

    [Fact]
    public async Task AnalyzeAsync_RecognizesGermanTestImageLocally()
    {
        Directory.CreateDirectory(directory);
        var pdfPath = Path.Combine(directory, "ocr-source.pdf");
        using (var builder = new PdfDocumentBuilder())
        {
            var font = builder.AddStandard14Font(Standard14Font.HelveticaBold);
            var page = builder.AddPage(PageSize.A4);
            page.AddText(
                "PROJEKTSTART 19.07.2026",
                32,
                new PdfPoint(70, 650),
                font);
            await File.WriteAllBytesAsync(pdfPath, builder.Build());
        }

        var preview = await new PdfiumPdfPreviewService().RenderPageAsync(
            pdfPath,
            pageNumber: 1,
            renderScale: 3,
            CancellationToken.None);
        var imagePath = Path.Combine(directory, "ocr-test.png");
        await File.WriteAllBytesAsync(imagePath, preview.PngData);
        var progress = new RecordingProgress<DocumentAnalysisProgress>();
        var analyzer = new ImageDocumentAnalyzer(ocrService);

        var result = await analyzer.AnalyzeAsync(
            imagePath,
            directory,
            progress,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.TechnicalDetails);
        Assert.Equal(TextExtractionMethod.Ocr, result.Value!.ExtractionMethod);
        Assert.Contains("PROJEKTSTART", result.Value.ExtractedText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("19.07.2026", result.Value.ExtractedText);
        Assert.Contains("19.07.2026", result.Value.DateSuggestions);
        Assert.Equal("de-DE", result.Value.Metadata["ocrLanguage"]);
        Assert.Contains(progress.Values, item => item.CompletedSteps == item.TotalSteps);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsOcrFailureForDamagedImage()
    {
        Directory.CreateDirectory(directory);
        var imagePath = Path.Combine(directory, "defekt.png");
        await File.WriteAllTextAsync(imagePath, "kein PNG");
        var analyzer = new ImageDocumentAnalyzer(ocrService);

        var result = await analyzer.AnalyzeAsync(
            imagePath,
            directory,
            progress: null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("OcrFailed", result.Error!.Code);
    }

    [Fact]
    public async Task AnalyzeAsync_PropagatesCancellation()
    {
        Directory.CreateDirectory(directory);
        var imagePath = Path.Combine(directory, "abbruch.png");
        await File.WriteAllBytesAsync(imagePath, [0x89, 0x50, 0x4E, 0x47]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var analyzer = new ImageDocumentAnalyzer(ocrService);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            analyzer.AnalyzeAsync(
                imagePath,
                directory,
                progress: null,
                cancellation.Token));
    }

    public void Dispose()
    {
        ocrService.Dispose();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class RecordingProgress<T> : IProgress<T>
    {
        public List<T> Values { get; } = [];

        public void Report(T value) => Values.Add(value);
    }
}
