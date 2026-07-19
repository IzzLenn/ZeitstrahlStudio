using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using ZeitstrahlStudio.DocumentProcessing;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class PdfiumPdfPreviewServiceTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        "ZeitstrahlStudio.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task RenderPageAsync_RendersStandardFontPageAsBoundedPng()
    {
        var path = await CreatePdfAsync();
        var service = new PdfiumPdfPreviewService();

        var result = await service.RenderPageAsync(
            path,
            pageNumber: 2,
            renderScale: 1,
            CancellationToken.None);

        Assert.Equal(2, result.PageNumber);
        Assert.Equal(2, result.PageCount);
        Assert.InRange(result.PixelWidth, 590, 600);
        Assert.InRange(result.PixelHeight, 840, 850);
        Assert.Equal(1, result.EffectiveRenderScale);
        Assert.True(result.PngData.AsSpan(0, 8).SequenceEqual(
            new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }));
    }

    [Fact]
    public async Task RenderPageAsync_RejectsPageOutsideDocument()
    {
        var path = await CreatePdfAsync();
        var service = new PdfiumPdfPreviewService();

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            service.RenderPageAsync(path, 3, 1, CancellationToken.None));

        Assert.Contains("Seite 3", exception.Message);
    }

    [Fact]
    public async Task RenderPageAsync_AppliesRequestedZoomScale()
    {
        var path = await CreatePdfAsync();
        var service = new PdfiumPdfPreviewService();

        var result = await service.RenderPageAsync(
            path,
            pageNumber: 1,
            renderScale: 1.5,
            CancellationToken.None);

        Assert.InRange(result.PixelWidth, 890, 900);
        Assert.InRange(result.PixelHeight, 1260, 1270);
        Assert.Equal(1.5, result.EffectiveRenderScale);
    }

    [Fact]
    public async Task RenderPageAsync_SupportsSmallScaleForWholePageFit()
    {
        var path = await CreatePdfAsync();
        var service = new PdfiumPdfPreviewService();

        var result = await service.RenderPageAsync(
            path,
            pageNumber: 1,
            renderScale: 0.02,
            CancellationToken.None);

        Assert.Equal(12, result.PixelWidth);
        Assert.Equal(17, result.PixelHeight);
        Assert.Equal(0.02, result.EffectiveRenderScale);
    }

    [Fact]
    public async Task RenderPageAsync_RejectsCorruptPdfAsPreviewError()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "defekt.pdf");
        await File.WriteAllTextAsync(path, "%PDF-kein-gueltiges-dokument");
        var service = new PdfiumPdfPreviewService();

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
            service.RenderPageAsync(path, 1, 1, CancellationToken.None));

        Assert.Contains("konnte nicht lokal dargestellt werden", exception.Message);
    }

    [Fact]
    public async Task RenderPageAsync_PropagatesCancellation()
    {
        var path = await CreatePdfAsync();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = new PdfiumPdfPreviewService();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.RenderPageAsync(path, 1, 1, cancellation.Token));
    }

    private async Task<string> CreatePdfAsync()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "vorschau.pdf");
        using var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var firstPage = builder.AddPage(PageSize.A4);
        firstPage.AddText("Erste Seite", 12, new PdfPoint(50, 750), font);
        var secondPage = builder.AddPage(PageSize.A4);
        secondPage.AddText("Zweite Seite", 12, new PdfPoint(50, 750), font);
        await File.WriteAllBytesAsync(path, builder.Build());
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
