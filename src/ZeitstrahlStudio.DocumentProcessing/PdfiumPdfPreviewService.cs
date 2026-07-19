using System.ComponentModel;
using PDFtoImage;
using PDFtoImage.Exceptions;
using SkiaSharp;
using ZeitstrahlStudio.Application;

namespace ZeitstrahlStudio.DocumentProcessing;

/// <summary>Rendert eine PDF-Seite über PDFium in eine begrenzte PNG-Vorschau.</summary>
public sealed class PdfiumPdfPreviewService : IPdfPreviewService
{
    private const long MaximumPixels = 24_000_000;
    private const int MaximumDimension = 8_000;
    private const int MaximumPngBytes = 100 * 1024 * 1024;
    private const int MaximumPages = 100_000;

    public Task<PdfPagePreview> RenderPageAsync(
        string validatedLocalPath,
        int pageNumber,
        double renderScale,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(validatedLocalPath))
        {
            throw new ArgumentException("Der PDF-Pfad darf nicht leer sein.", nameof(validatedLocalPath));
        }

        if (!double.IsFinite(renderScale) || renderScale <= 0 || renderScale > 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(renderScale),
                "Der PDF-Renderfaktor muss größer als 0 und höchstens 4 sein.");
        }

        return Task.Run(
            () => RenderPage(
                Path.GetFullPath(validatedLocalPath),
                pageNumber,
                renderScale,
                cancellationToken),
            cancellationToken);
    }

    private static PdfPagePreview RenderPage(
        string path,
        int pageNumber,
        double requestedScale,
        CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "Die integrierte PDF-Vorschau wird nur unter Windows unterstützt.");
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var input = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                FileOptions.RandomAccess);

            var pageCount = Conversion.GetPageCount(input, leaveOpen: true);
            if (pageCount is < 1 or > MaximumPages)
            {
                throw new InvalidDataException(
                    $"Das PDF enthält eine unzulässige Seitenzahl ({pageCount}).");
            }

            if (pageNumber < 1 || pageNumber > pageCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pageNumber),
                    $"Die PDF-Seite muss zwischen 1 und {pageCount} liegen.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var pageIndex = new Index(pageNumber - 1, fromEnd: false);
            var pageSize = Conversion.GetPageSize(input, pageIndex, leaveOpen: true);
            var effectiveScale = GetSafeScale(pageSize.Width, pageSize.Height, requestedScale);
            var pixelWidth = GetPixelDimension(pageSize.Width, effectiveScale);
            var pixelHeight = GetPixelDimension(pageSize.Height, effectiveScale);
            var options = new RenderOptions(
                Dpi: 72,
                Width: pixelWidth,
                Height: pixelHeight,
                WithAnnotations: true,
                WithFormFill: true,
                UseTiling: pixelWidth > 4_000 || pixelHeight > 4_000);

            cancellationToken.ThrowIfCancellationRequested();
            using var bitmap = Conversion.ToImage(
                input,
                pageIndex,
                leaveOpen: true,
                options: options);
            cancellationToken.ThrowIfCancellationRequested();

            if (bitmap.Width != pixelWidth || bitmap.Height != pixelHeight)
            {
                throw new InvalidDataException(
                    "Die PDF-Seite wurde mit einer unerwarteten Vorschaugröße gerendert.");
            }

            using var data = bitmap.Encode(SKEncodedImageFormat.Png, quality: 100);
            var png = data.ToArray();
            if (png.Length > MaximumPngBytes)
            {
                throw new InvalidDataException(
                    "Die gerenderte PDF-Seite überschreitet das Vorschaugrößenlimit.");
            }

            return new PdfPagePreview(
                pageNumber,
                pageCount,
                pixelWidth,
                pixelHeight,
                effectiveScale,
                png);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is IOException or
                UnauthorizedAccessException or
                ArgumentException or
                InvalidOperationException or
                OverflowException or
                Win32Exception or
                PdfException)
        {
            throw new InvalidDataException(
                $"Die PDF-Seite {pageNumber} konnte nicht lokal dargestellt werden.",
                exception);
        }
    }

    private static double GetSafeScale(double width, double height, double requestedScale)
    {
        if (!double.IsFinite(width) ||
            !double.IsFinite(height) ||
            width <= 0 ||
            height <= 0)
        {
            throw new InvalidDataException("Das PDF enthält eine ungültige Seitengröße.");
        }

        var dimensionScale = Math.Min(
            MaximumDimension / width,
            MaximumDimension / height);
        var pixelScale = Math.Sqrt(MaximumPixels / (width * height));
        var effectiveScale = Math.Min(requestedScale, Math.Min(dimensionScale, pixelScale));
        if (!double.IsFinite(effectiveScale) || effectiveScale <= 0)
        {
            throw new InvalidDataException(
                "Die PDF-Seite ist für eine sichere integrierte Vorschau zu groß.");
        }

        return effectiveScale;
    }

    private static int GetPixelDimension(double pageDimension, double renderScale)
    {
        var scaledDimension = pageDimension * renderScale;
        if (!double.IsFinite(scaledDimension) || scaledDimension <= 0)
        {
            throw new InvalidDataException("Die PDF-Vorschaugröße ist ungültig.");
        }

        return checked((int)Math.Max(1, Math.Ceiling(scaledDimension)));
    }
}
