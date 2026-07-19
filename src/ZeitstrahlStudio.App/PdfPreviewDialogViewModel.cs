using System.IO;
using System.Windows.Media.Imaging;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Steuert Seitennavigation, Zoom und lokale Darstellung des PDF-Vorschaufensters.</summary>
public sealed class PdfPreviewDialogViewModel : ObservableObject, IDisposable
{
    private const double MinimumInteractiveRenderScale = 0.05;
    private const double MaximumRenderScale = 4;
    private readonly IPdfPreviewService previewService;
    private readonly ILocalLogService logService;
    private readonly string validatedLocalPath;
    private readonly Func<CancellationToken, Task> openExternallyAsync;
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private BitmapSource? previewImage;
    private int pageNumber;
    private int pageCount;
    private double requestedRenderScale = 1;
    private double effectiveRenderScale = 1;
    private double pageWidthAtScaleOne;
    private double pageHeightAtScaleOne;
    private double viewportWidth;
    private double viewportHeight;
    private double previewWidth;
    private double previewHeight;
    private bool isBusy;
    private bool disposed;
    private string statusMessage = "PDF wird vorbereitet …";
    private string errorMessage = string.Empty;
    private string dimensionsText = string.Empty;

    public PdfPreviewDialogViewModel(
        IPdfPreviewService previewService,
        ILocalLogService logService,
        Attachment attachment,
        string validatedLocalPath,
        Func<CancellationToken, Task> openExternallyAsync)
    {
        this.previewService = previewService ?? throw new ArgumentNullException(nameof(previewService));
        this.logService = logService ?? throw new ArgumentNullException(nameof(logService));
        ArgumentNullException.ThrowIfNull(attachment);
        this.validatedLocalPath = string.IsNullOrWhiteSpace(validatedLocalPath)
            ? throw new ArgumentException("Der PDF-Pfad darf nicht leer sein.", nameof(validatedLocalPath))
            : validatedLocalPath;
        this.openExternallyAsync = openExternallyAsync ??
            throw new ArgumentNullException(nameof(openExternallyAsync));

        FileName = attachment.OriginalFileName;
        InitialPageNumber = attachment.LinkedPdfPage ?? 1;
        PreviousPageCommand = new AsyncRelayCommand(
            () => LoadPageAsync(PageNumber - 1, requestedRenderScale, lifetimeCancellation.Token),
            () => !IsBusy && PageNumber > 1);
        NextPageCommand = new AsyncRelayCommand(
            () => LoadPageAsync(PageNumber + 1, requestedRenderScale, lifetimeCancellation.Token),
            () => !IsBusy && PageCount > 0 && PageNumber < PageCount);
        ZoomOutCommand = new AsyncRelayCommand(
            () => ChangeZoomAsync(1 / 1.25),
            () => !IsBusy && PreviewImage is not null && requestedRenderScale > MinimumInteractiveRenderScale);
        ZoomInCommand = new AsyncRelayCommand(
            () => ChangeZoomAsync(1.25),
            () => !IsBusy && PreviewImage is not null && requestedRenderScale < MaximumRenderScale);
        FitWidthCommand = new AsyncRelayCommand(
            FitWidthAsync,
            () => CanFitToViewport);
        ShowWholePageCommand = new AsyncRelayCommand(
            ShowWholePageAsync,
            () => CanFitToViewport);
        OpenExternallyCommand = new AsyncRelayCommand(
            OpenExternallyGuardedAsync,
            () => !IsBusy);
    }

    public string FileName { get; }
    public int InitialPageNumber { get; }
    public AsyncRelayCommand PreviousPageCommand { get; }
    public AsyncRelayCommand NextPageCommand { get; }
    public AsyncRelayCommand ZoomOutCommand { get; }
    public AsyncRelayCommand ZoomInCommand { get; }
    public AsyncRelayCommand FitWidthCommand { get; }
    public AsyncRelayCommand ShowWholePageCommand { get; }
    public AsyncRelayCommand OpenExternallyCommand { get; }

    public BitmapSource? PreviewImage
    {
        get => previewImage;
        private set
        {
            if (SetProperty(ref previewImage, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public int PageNumber
    {
        get => pageNumber;
        private set
        {
            if (SetProperty(ref pageNumber, value))
            {
                OnPropertyChanged(nameof(PageDisplay));
                RaiseCommandStates();
            }
        }
    }

    public int PageCount
    {
        get => pageCount;
        private set
        {
            if (SetProperty(ref pageCount, value))
            {
                OnPropertyChanged(nameof(PageDisplay));
                RaiseCommandStates();
            }
        }
    }

    public string PageDisplay => PageCount > 0
        ? $"Seite {PageNumber} von {PageCount}"
        : "Seite –";

    public string ZoomDisplay => $"Zoom: {effectiveRenderScale * 100:0}%";

    public double PreviewWidth
    {
        get => previewWidth;
        private set => SetProperty(ref previewWidth, value);
    }

    public double PreviewHeight
    {
        get => previewHeight;
        private set => SetProperty(ref previewHeight, value);
    }

    public string DimensionsText
    {
        get => dimensionsText;
        private set => SetProperty(ref dimensionsText, value);
    }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetProperty(ref isBusy, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetProperty(ref statusMessage, value);
    }

    public string ErrorMessage
    {
        get => errorMessage;
        private set
        {
            if (SetProperty(ref errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    private bool CanFitToViewport =>
        !IsBusy &&
        PreviewImage is not null &&
        viewportWidth > 80 &&
        viewportHeight > 80 &&
        pageWidthAtScaleOne > 0 &&
        pageHeightAtScaleOne > 0;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var loaded = await LoadPageAsync(
            InitialPageNumber,
            requestedRenderScale,
            cancellationToken).ConfigureAwait(true);
        if (!loaded && InitialPageNumber != 1 && !cancellationToken.IsCancellationRequested)
        {
            loaded = await LoadPageAsync(1, requestedRenderScale, cancellationToken).ConfigureAwait(true);
            if (loaded)
            {
                StatusMessage =
                    $"Die verknüpfte Seite {InitialPageNumber} ist nicht verfügbar; Seite 1 wird angezeigt.";
            }
        }
    }

    /// <summary>Lädt nach atomarer Aktualisierung derselben PDF-Datei wieder deren erste Seite.</summary>
    public Task<bool> ReloadAsync(CancellationToken cancellationToken) =>
        LoadPageAsync(1, requestedRenderScale, cancellationToken);

    public void UpdateViewport(double width, double height)
    {
        if (!double.IsFinite(width) || !double.IsFinite(height))
        {
            return;
        }

        viewportWidth = Math.Max(0, width);
        viewportHeight = Math.Max(0, height);
        RaiseCommandStates();
    }

    private async Task<bool> LoadPageAsync(
        int targetPageNumber,
        double targetRenderScale,
        CancellationToken cancellationToken)
    {
        if (disposed)
        {
            return false;
        }

        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            lifetimeCancellation.Token);
        IsBusy = true;
        ErrorMessage = string.Empty;
        StatusMessage = $"Seite {targetPageNumber} wird lokal gerendert …";
        try
        {
            var preview = await previewService.RenderPageAsync(
                validatedLocalPath,
                targetPageNumber,
                targetRenderScale,
                linkedCancellation.Token).ConfigureAwait(true);
            linkedCancellation.Token.ThrowIfCancellationRequested();

            PreviewImage = DecodePng(preview.PngData);
            PageNumber = preview.PageNumber;
            PageCount = preview.PageCount;
            requestedRenderScale = targetRenderScale;
            effectiveRenderScale = preview.EffectiveRenderScale;
            pageWidthAtScaleOne = preview.PixelWidth / preview.EffectiveRenderScale;
            pageHeightAtScaleOne = preview.PixelHeight / preview.EffectiveRenderScale;
            PreviewWidth = preview.PixelWidth;
            PreviewHeight = preview.PixelHeight;
            DimensionsText = $"{preview.PixelWidth} × {preview.PixelHeight} Pixel";
            OnPropertyChanged(nameof(ZoomDisplay));
            StatusMessage = $"Seite {preview.PageNumber} von {preview.PageCount} ist bereit.";
            return true;
        }
        catch (OperationCanceledException) when (lifetimeCancellation.IsCancellationRequested)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception is InvalidDataException
                ? exception.Message
                : "Die PDF-Seite konnte nicht dargestellt werden.";
            StatusMessage = "PDF-Vorschau fehlgeschlagen.";
            await LogErrorAsync("PdfPreviewRenderFailed", ErrorMessage, exception).ConfigureAwait(true);
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task ChangeZoomAsync(double factor)
    {
        var targetScale = Math.Clamp(
            Math.Round(requestedRenderScale * factor, 3),
            MinimumInteractiveRenderScale,
            MaximumRenderScale);
        return LoadPageAsync(PageNumber, targetScale, lifetimeCancellation.Token);
    }

    private Task FitWidthAsync()
    {
        var targetScale = Math.Min(
            (viewportWidth - 32) / pageWidthAtScaleOne,
            MaximumRenderScale);
        return LoadPageAsync(PageNumber, targetScale, lifetimeCancellation.Token);
    }

    private Task ShowWholePageAsync()
    {
        var targetScale = Math.Min(
            Math.Min(
                (viewportWidth - 32) / pageWidthAtScaleOne,
                (viewportHeight - 32) / pageHeightAtScaleOne),
            MaximumRenderScale);
        return LoadPageAsync(PageNumber, targetScale, lifetimeCancellation.Token);
    }

    private async Task OpenExternallyGuardedAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        StatusMessage = "PDF wird vor dem Öffnen erneut geprüft …";
        try
        {
            await openExternallyAsync(lifetimeCancellation.Token).ConfigureAwait(true);
            StatusMessage = "PDF wurde an das Windows-Standardprogramm übergeben.";
        }
        catch (OperationCanceledException) when (lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            ErrorMessage = "Die PDF-Datei konnte nicht im Windows-Standardprogramm geöffnet werden.";
            StatusMessage = "Öffnen der PDF-Datei fehlgeschlagen.";
            await LogErrorAsync("PdfPreviewExternalOpenFailed", ErrorMessage, exception).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LogErrorAsync(string eventName, string message, Exception exception)
    {
        try
        {
            await logService.WriteAsync(
                new LocalLogEntry(
                    DateTimeOffset.UtcNow,
                    LocalLogLevel.Error,
                    nameof(PdfPreviewDialogViewModel),
                    eventName,
                    message,
                    exception.ToString()),
                CancellationToken.None).ConfigureAwait(true);
        }
        catch (Exception logException) when (logException is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static BitmapImage DecodePng(byte[] pngData)
    {
        using var stream = new MemoryStream(pngData, writable: false);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private void RaiseCommandStates()
    {
        PreviousPageCommand.RaiseCanExecuteChanged();
        NextPageCommand.RaiseCanExecuteChanged();
        ZoomOutCommand.RaiseCanExecuteChanged();
        ZoomInCommand.RaiseCanExecuteChanged();
        FitWidthCommand.RaiseCanExecuteChanged();
        ShowWholePageCommand.RaiseCanExecuteChanged();
        OpenExternallyCommand.RaiseCanExecuteChanged();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        lifetimeCancellation.Cancel();
        lifetimeCancellation.Dispose();
    }
}
