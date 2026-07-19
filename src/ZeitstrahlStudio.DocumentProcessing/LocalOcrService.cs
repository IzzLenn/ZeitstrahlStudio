using System.Runtime.InteropServices;
using System.Text;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;
using ZeitstrahlStudio.Application;

namespace ZeitstrahlStudio.DocumentProcessing;

/// <summary>Ergebnis einer vollständig lokalen OCR-Ausführung.</summary>
public sealed record LocalOcrResult(
    string Text,
    string LanguageTag,
    int PageCount,
    int MaximumPixelWidth,
    int MaximumPixelHeight);

/// <summary>Abstrahiert die lokale Windows-OCR für Bilder und gerenderte PDF-Seiten.</summary>
public interface ILocalOcrService
{
    Task<LocalOcrResult> RecognizeFileAsync(
        string localFilePath,
        IProgress<DocumentAnalysisProgress>? progress,
        CancellationToken cancellationToken);

    Task<LocalOcrResult> RecognizePngAsync(
        byte[] pngData,
        CancellationToken cancellationToken);
}

/// <summary>Nutzt ausschließlich die auf dem Gerät installierte Windows-OCR-Sprachressource.</summary>
public sealed class WindowsLocalOcrService : ILocalOcrService, IDisposable
{
    private const long MaximumCompressedImageBytes = 512L * 1024 * 1024;
    private const ulong MaximumDecodedPixelsPerPage = 24_000_000;
    private const uint MaximumImageFrames = 250;
    private const int MaximumExtractedCharacters = 10_000_000;
    private readonly SemaphoreSlim recognitionGate = new(1, 1);
    private OcrEngine? engine;
    private bool disposed;

    public async Task<LocalOcrResult> RecognizeFileAsync(
        string localFilePath,
        IProgress<DocumentAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localFilePath);
        cancellationToken.ThrowIfCancellationRequested();
        var path = Path.GetFullPath(localFilePath);
        var fileInfo = new FileInfo(path);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException("Die Bild-Projektkopie wurde nicht gefunden.", path);
        }

        if (fileInfo.Length is <= 0 or > MaximumCompressedImageBytes)
        {
            throw new InvalidDataException(
                "Die Bilddatei ist leer oder überschreitet das OCR-Sicherheitslimit von 512 MiB.");
        }

        var storageFile = await StorageFile.GetFileFromPathAsync(path)
            .AsTask(cancellationToken).ConfigureAwait(false);
        using var stream = await storageFile.OpenReadAsync()
            .AsTask(cancellationToken).ConfigureAwait(false);
        return await RecognizeStreamAsync(stream, progress, cancellationToken).ConfigureAwait(false);
    }

    public async Task<LocalOcrResult> RecognizePngAsync(
        byte[] pngData,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pngData);
        if (pngData.Length is 0 or > 100 * 1024 * 1024)
        {
            throw new InvalidDataException(
                "Die gerenderte PDF-Seite ist leer oder überschreitet das OCR-Sicherheitslimit.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new InMemoryRandomAccessStream();
        using (var output = stream.GetOutputStreamAt(0))
        using (var writer = new DataWriter(output))
        {
            writer.WriteBytes(pngData);
            await writer.StoreAsync().AsTask(cancellationToken).ConfigureAwait(false);
            writer.DetachStream();
        }

        stream.Seek(0);
        return await RecognizeStreamAsync(stream, progress: null, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        recognitionGate.Dispose();
        disposed = true;
        GC.SuppressFinalize(this);
    }

    private async Task<LocalOcrResult> RecognizeStreamAsync(
        IRandomAccessStream stream,
        IProgress<DocumentAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await recognitionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var localEngine = GetOrCreateGermanEngine();
            var decoder = await BitmapDecoder.CreateAsync(stream)
                .AsTask(cancellationToken).ConfigureAwait(false);
            if (decoder.FrameCount is 0 or > MaximumImageFrames)
            {
                throw new InvalidDataException(
                    $"Das Bild enthält eine unzulässige Seitenzahl ({decoder.FrameCount}).");
            }

            var text = new StringBuilder();
            var maximumWidth = 0;
            var maximumHeight = 0;
            for (uint frameIndex = 0; frameIndex < decoder.FrameCount; frameIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new DocumentAnalysisProgress(
                    $"OCR-Seite {frameIndex + 1} von {decoder.FrameCount}",
                    checked((int)frameIndex),
                    checked((int)decoder.FrameCount)));
                IBitmapFrameWithSoftwareBitmap frame = frameIndex == 0
                    ? decoder
                    : await decoder.GetFrameAsync(frameIndex)
                        .AsTask(cancellationToken).ConfigureAwait(false);
                var dimensions = CalculateTargetDimensions(
                    frame.OrientedPixelWidth,
                    frame.OrientedPixelHeight);
                maximumWidth = Math.Max(maximumWidth, dimensions.Width);
                maximumHeight = Math.Max(maximumHeight, dimensions.Height);
                var transform = new BitmapTransform
                {
                    ScaledWidth = checked((uint)dimensions.Width),
                    ScaledHeight = checked((uint)dimensions.Height),
                };
                using var bitmap = await frame.GetSoftwareBitmapAsync(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Premultiplied,
                        transform,
                        ExifOrientationMode.RespectExifOrientation,
                        ColorManagementMode.ColorManageToSRgb)
                    .AsTask(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                var result = await localEngine.RecognizeAsync(bitmap)
                    .AsTask(cancellationToken).ConfigureAwait(false);
                AppendLimited(text, result.Text);
                if (frameIndex + 1 < decoder.FrameCount && text.Length > 0)
                {
                    AppendLimited(text, Environment.NewLine + Environment.NewLine);
                }

                progress?.Report(new DocumentAnalysisProgress(
                    $"OCR-Seite {frameIndex + 1} von {decoder.FrameCount}",
                    checked((int)frameIndex + 1),
                    checked((int)decoder.FrameCount)));
            }

            return new LocalOcrResult(
                text.ToString().Trim(),
                localEngine.RecognizerLanguage.LanguageTag,
                checked((int)decoder.FrameCount),
                maximumWidth,
                maximumHeight);
        }
        catch (COMException exception)
        {
            throw new InvalidDataException(
                "Das Bild konnte von der lokalen Windows-OCR nicht verarbeitet werden.",
                exception);
        }
        finally
        {
            recognitionGate.Release();
        }
    }

    private OcrEngine GetOrCreateGermanEngine()
    {
        if (engine is not null)
        {
            return engine;
        }

        var german = OcrEngine.AvailableRecognizerLanguages.FirstOrDefault(language =>
            language.LanguageTag.Equals("de-DE", StringComparison.OrdinalIgnoreCase) ||
            language.LanguageTag.StartsWith("de-", StringComparison.OrdinalIgnoreCase));
        if (german is null)
        {
            throw new InvalidOperationException(
                "Die deutsche Windows-OCR-Sprachressource ist nicht installiert. " +
                "Installieren Sie in den Windows-Einstellungen das deutsche Sprachpaket " +
                "einschließlich Texterkennung und starten Sie die Analyse erneut.");
        }

        engine = OcrEngine.TryCreateFromLanguage(new Language(german.LanguageTag));
        return engine ?? throw new InvalidOperationException(
            "Die installierte deutsche Windows-OCR-Sprachressource konnte nicht gestartet werden.");
    }

    private static (int Width, int Height) CalculateTargetDimensions(uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            throw new InvalidDataException("Das Bild besitzt keine gültigen Pixelabmessungen.");
        }

        var engineLimit = OcrEngine.MaxImageDimension;
        var pixelCount = (double)width * height;
        var scale = Math.Min(
            1d,
            Math.Min(
                engineLimit / (double)Math.Max(width, height),
                Math.Sqrt(MaximumDecodedPixelsPerPage / pixelCount)));
        var targetWidth = Math.Max(1, checked((int)Math.Floor(width * scale)));
        var targetHeight = Math.Max(1, checked((int)Math.Floor(height * scale)));
        return (targetWidth, targetHeight);
    }

    private static void AppendLimited(StringBuilder target, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (target.Length + value.Length > MaximumExtractedCharacters)
        {
            throw new InvalidDataException(
                "Der erkannte OCR-Text überschreitet das Sicherheitslimit.");
        }

        target.Append(value);
    }
}
