using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.DocumentProcessing;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class PdfDocumentAnalyzerTests : IDisposable
{
    private readonly WindowsLocalOcrService ocrService = new();
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

        var analyzer = new PdfDocumentAnalyzer(ocrService, new PdfiumPdfPreviewService());
        var result = await analyzer.AnalyzeAsync(
            path,
            directory,
            progress: null,
            CancellationToken.None);

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
    public async Task AnalyzeAsync_UsesOcrForImageOnlyPage()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "scan.pdf");
        using (var builder = new PdfDocumentBuilder())
        {
            builder.AddPage(PageSize.A4);
            await File.WriteAllBytesAsync(path, builder.Build());
        }

        var fakeOcr = new FakeOcrService("Gescannter Termin 21.07.2026");
        var analyzer = new PdfDocumentAnalyzer(fakeOcr, new PdfiumPdfPreviewService());
        var progress = new RecordingProgress<DocumentAnalysisProgress>();

        var result = await analyzer.AnalyzeAsync(
            path,
            directory,
            progress,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.TechnicalDetails);
        Assert.Equal(TextExtractionMethod.Ocr, result.Value!.ExtractionMethod);
        Assert.Equal("Gescannter Termin 21.07.2026", result.Value.ExtractedText);
        Assert.Contains("21.07.2026", result.Value.DateSuggestions);
        Assert.Equal("1", result.Value.Metadata["ocrPages"]);
        Assert.Equal(1, fakeOcr.CallCount);
        Assert.Contains(progress.Values, item => item.CompletedSteps == item.TotalSteps);
    }

    [Fact]
    public async Task AnalyzeAsync_CombinesEmbeddedTextAndOcrForMixedPdf()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "gemischt.pdf");
        using (var builder = new PdfDocumentBuilder())
        {
            var font = builder.AddStandard14Font(Standard14Font.Helvetica);
            var textPage = builder.AddPage(PageSize.A4);
            textPage.AddText("Direkter PDF-Text", 18, new PdfPoint(50, 700), font);
            builder.AddPage(PageSize.A4);
            await File.WriteAllBytesAsync(path, builder.Build());
        }

        var fakeOcr = new FakeOcrService("Text der Scan-Seite");
        var analyzer = new PdfDocumentAnalyzer(fakeOcr, new PdfiumPdfPreviewService());

        var result = await analyzer.AnalyzeAsync(
            path,
            directory,
            progress: null,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.TechnicalDetails);
        Assert.Equal(TextExtractionMethod.EmbeddedTextAndOcr, result.Value!.ExtractionMethod);
        Assert.Contains("Direkter PDF-Text", result.Value.ExtractedText);
        Assert.Contains("Text der Scan-Seite", result.Value.ExtractedText);
        Assert.Equal("2", result.Value.Metadata["ocrPages"]);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsFailureForDamagedPdf()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "defekt.pdf");
        await File.WriteAllTextAsync(path, "%PDF-1.7 beschädigt");
        var analyzer = new PdfDocumentAnalyzer(ocrService, new PdfiumPdfPreviewService());

        var result = await analyzer.AnalyzeAsync(
            path,
            directory,
            progress: null,
            CancellationToken.None);

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
        var analyzer = new PdfDocumentAnalyzer(ocrService, new PdfiumPdfPreviewService());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            analyzer.AnalyzeAsync(path, directory, progress: null, cancellation.Token));
    }

    public void Dispose()
    {
        ocrService.Dispose();
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class FakeOcrService(string text) : ILocalOcrService
    {
        private int callCount;

        public int CallCount => Volatile.Read(ref callCount);

        public Task<LocalOcrResult> RecognizeFileAsync(
            string localFilePath,
            IProgress<DocumentAnalysisProgress>? progress,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<LocalOcrResult> RecognizePngAsync(
            byte[] pngData,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref callCount);
            return Task.FromResult(new LocalOcrResult(text, "de-DE", 1, 1785, 2526));
        }
    }

    private sealed class RecordingProgress<T> : IProgress<T>
    {
        public List<T> Values { get; } = [];

        public void Report(T value) => Values.Add(value);
    }
}
