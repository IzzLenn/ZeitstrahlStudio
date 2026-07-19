using System.Diagnostics;
using System.IO;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

public enum PdfExportMode
{
    MultiplePages,
    SingleLargePage,
    SelectedRange,
}

public sealed record PdfExportModeChoice(PdfExportMode Value, string DisplayName);

/// <summary>Steuert Exportoptionen, echte PDF-Vorschau und atomaren Zieldateiexport.</summary>
public sealed class PdfExportDialogViewModel : ObservableObject, IDisposable
{
    private readonly IPdfExportService exportService;
    private readonly ILocalLogService logService;
    private readonly ProjectWorkspace workspace;
    private readonly Func<string?> requestTargetPath;
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private readonly string previewDirectory;
    private readonly string previewPath;
    private PdfExportOptions? lastPreviewOptions;
    private string selectedPaperSize = "A4";
    private bool landscape;
    private double widthMillimeters = 210;
    private double heightMillimeters = 297;
    private double fontSize;
    private PdfExportModeChoice selectedMode;
    private DateTime? rangeStart;
    private DateTime? rangeEnd;
    private bool includeOverlappingRanges = true;
    private bool includeNotes;
    private bool isBusy;
    private bool previewReady;
    private bool disposed;
    private string statusMessage = "Exportvorschau wird vorbereitet …";
    private string warningsText = string.Empty;
    private string errorMessage = string.Empty;
    private string pageSummary = string.Empty;

    public PdfExportDialogViewModel(
        IPdfExportService exportService,
        IPdfPreviewService previewService,
        ILocalLogService logService,
        ProjectWorkspace workspace,
        Func<string?> requestTargetPath)
    {
        this.exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        this.logService = logService ?? throw new ArgumentNullException(nameof(logService));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.requestTargetPath = requestTargetPath ?? throw new ArgumentNullException(nameof(requestTargetPath));
        fontSize = workspace.Project.Settings.ExportFontSize;
        Modes =
        [
            new PdfExportModeChoice(PdfExportMode.MultiplePages, "Mehrseitiger Export"),
            new PdfExportModeChoice(PdfExportMode.SingleLargePage, "Große Einzelseite"),
            new PdfExportModeChoice(PdfExportMode.SelectedRange, "Ausgewählter Zeitraum"),
        ];
        selectedMode = Modes[0];

        var chronological = workspace.Project.GetChronologicalEvents();
        if (chronological.Count > 0)
        {
            rangeStart = chronological[0].Date.SortStart.Date;
            var lastDate = chronological[^1].Date;
            rangeEnd = lastDate.EndYear.HasValue
                ? new DateTime(lastDate.EndYear.Value, lastDate.EndMonth!.Value, lastDate.EndDay!.Value)
                : lastDate.SortStart.Date;
        }

        previewDirectory = Path.Combine(
            Path.GetTempPath(),
            "ZeitstrahlStudio",
            "ExportPreview",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(previewDirectory);
        previewPath = Path.Combine(previewDirectory, "Exportvorschau.pdf");
        var previewAttachment = new Attachment(
            Guid.NewGuid(),
            "Exportvorschau.pdf",
            "application/pdf",
            0,
            new string('0', 64),
            null,
            DateTimeOffset.UtcNow,
            "Exportvorschau.pdf");
        Preview = new PdfPreviewDialogViewModel(
            previewService,
            logService,
            previewAttachment,
            previewPath,
            OpenPreviewExternallyAsync);

        RefreshPreviewCommand = new AsyncRelayCommand(RefreshPreviewAsync, () => !IsBusy);
        ExportCommand = new AsyncRelayCommand(ExportAsync, () => !IsBusy && PreviewReady);
    }

    public event EventHandler? ExportCompleted;

    public IReadOnlyList<string> PaperSizes { get; } = ["A4", "A3", "Letter", "Benutzerdefiniert"];
    public IReadOnlyList<PdfExportModeChoice> Modes { get; }
    public PdfPreviewDialogViewModel Preview { get; }
    public AsyncRelayCommand RefreshPreviewCommand { get; }
    public AsyncRelayCommand ExportCommand { get; }
    public string? ExportedTargetPath { get; private set; }

    public string SelectedPaperSize
    {
        get => selectedPaperSize;
        set
        {
            if (SetProperty(ref selectedPaperSize, value))
            {
                OnPropertyChanged(nameof(IsCustomPaper));
                MarkPreviewOutdated();
            }
        }
    }

    public bool IsCustomPaper => SelectedPaperSize == "Benutzerdefiniert";

    public bool Landscape
    {
        get => landscape;
        set
        {
            if (SetProperty(ref landscape, value))
            {
                MarkPreviewOutdated();
            }
        }
    }

    public double WidthMillimeters
    {
        get => widthMillimeters;
        set
        {
            if (SetProperty(ref widthMillimeters, value))
            {
                MarkPreviewOutdated();
            }
        }
    }

    public double HeightMillimeters
    {
        get => heightMillimeters;
        set
        {
            if (SetProperty(ref heightMillimeters, value))
            {
                MarkPreviewOutdated();
            }
        }
    }

    public double FontSize
    {
        get => fontSize;
        set
        {
            if (SetProperty(ref fontSize, value))
            {
                MarkPreviewOutdated();
            }
        }
    }

    public PdfExportModeChoice SelectedMode
    {
        get => selectedMode;
        set
        {
            if (SetProperty(ref selectedMode, value))
            {
                OnPropertyChanged(nameof(IsRangeMode));
                MarkPreviewOutdated();
            }
        }
    }

    public bool IsRangeMode => SelectedMode.Value == PdfExportMode.SelectedRange;

    public DateTime? RangeStart
    {
        get => rangeStart;
        set
        {
            if (SetProperty(ref rangeStart, value))
            {
                MarkPreviewOutdated();
            }
        }
    }

    public DateTime? RangeEnd
    {
        get => rangeEnd;
        set
        {
            if (SetProperty(ref rangeEnd, value))
            {
                MarkPreviewOutdated();
            }
        }
    }

    public bool IncludeOverlappingRanges
    {
        get => includeOverlappingRanges;
        set
        {
            if (SetProperty(ref includeOverlappingRanges, value))
            {
                MarkPreviewOutdated();
            }
        }
    }

    public bool IncludeNotes
    {
        get => includeNotes;
        set
        {
            if (SetProperty(ref includeNotes, value))
            {
                MarkPreviewOutdated();
            }
        }
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

    public bool PreviewReady
    {
        get => previewReady;
        private set
        {
            if (SetProperty(ref previewReady, value))
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

    public string WarningsText
    {
        get => warningsText;
        private set
        {
            if (SetProperty(ref warningsText, value))
            {
                OnPropertyChanged(nameof(HasWarnings));
            }
        }
    }

    public bool HasWarnings => !string.IsNullOrWhiteSpace(WarningsText);

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

    public string PageSummary
    {
        get => pageSummary;
        private set => SetProperty(ref pageSummary, value);
    }

    public Task InitializeAsync(CancellationToken cancellationToken) =>
        RefreshPreviewCoreAsync(cancellationToken);

    private Task RefreshPreviewAsync() => RefreshPreviewCoreAsync(lifetimeCancellation.Token);

    private async Task RefreshPreviewCoreAsync(CancellationToken cancellationToken)
    {
        if (disposed)
        {
            return;
        }

        IsBusy = true;
        PreviewReady = false;
        ErrorMessage = string.Empty;
        StatusMessage = "Druckoptimierte PDF-Vorschau wird erzeugt …";
        try
        {
            var options = CreateOptions();
            var preview = await exportService.CreatePreviewAsync(
                workspace,
                options,
                cancellationToken).ConfigureAwait(true);
            await exportService.ExportAsync(
                workspace,
                options,
                previewPath,
                cancellationToken).ConfigureAwait(true);
            if (lastPreviewOptions is null)
            {
                await Preview.InitializeAsync(cancellationToken).ConfigureAwait(true);
            }
            else
            {
                _ = await Preview.ReloadAsync(cancellationToken).ConfigureAwait(true);
            }

            lastPreviewOptions = options;
            WarningsText = string.Join(Environment.NewLine, preview.Warnings.Select(warning => "• " + warning));
            PageSummary = preview.PageCount == 1
                ? $"1 Seite · {preview.PageWidthMillimeters:0.#} × {preview.PageHeightMillimeters:0.#} mm"
                : $"{preview.PageCount} Seiten · {preview.PageWidthMillimeters:0.#} × {preview.PageHeightMillimeters:0.#} mm";
            PreviewReady = !Preview.HasError;
            StatusMessage = PreviewReady
                ? "Vorschau ist aktuell."
                : "Die PDF wurde geplant, konnte aber nicht dargestellt werden.";
        }
        catch (OperationCanceledException) when (lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            StatusMessage = "Exportvorschau konnte nicht erzeugt werden.";
            await LogErrorAsync("PdfExportPreviewFailed", ErrorMessage, exception).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportAsync()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var options = CreateOptions();
            if (lastPreviewOptions != options)
            {
                IsBusy = false;
                await RefreshPreviewCoreAsync(lifetimeCancellation.Token).ConfigureAwait(true);
                if (!PreviewReady)
                {
                    return;
                }

                IsBusy = true;
            }

            var targetPath = requestTargetPath();
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                StatusMessage = "PDF-Export wurde nicht gespeichert.";
                return;
            }

            StatusMessage = "PDF-Datei wird vollständig lokal gespeichert …";
            await exportService.ExportAsync(
                workspace,
                options,
                targetPath,
                lifetimeCancellation.Token).ConfigureAwait(true);
            ExportedTargetPath = targetPath;
            StatusMessage = "PDF-Datei wurde erfolgreich gespeichert.";
            ExportCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) when (lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            StatusMessage = "PDF-Export ist fehlgeschlagen.";
            await LogErrorAsync("PdfExportFailed", ErrorMessage, exception).ConfigureAwait(true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private PdfExportOptions CreateOptions()
    {
        var isRange = SelectedMode.Value == PdfExportMode.SelectedRange;
        return new PdfExportOptions(
            SelectedPaperSize,
            Landscape,
            WidthMillimeters,
            HeightMillimeters,
            FontSize,
            isRange && RangeStart.HasValue ? DateOnly.FromDateTime(RangeStart.Value) : null,
            isRange && RangeEnd.HasValue ? DateOnly.FromDateTime(RangeEnd.Value) : null,
            IncludeOverlappingRanges,
            SelectedMode.Value == PdfExportMode.SingleLargePage,
            IncludeNotes);
    }

    private void MarkPreviewOutdated()
    {
        PreviewReady = false;
        StatusMessage = "Einstellungen geändert – bitte Vorschau aktualisieren.";
    }

    private Task OpenPreviewExternallyAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Process.Start(new ProcessStartInfo(previewPath) { UseShellExecute = true });
        return Task.CompletedTask;
    }

    private async Task LogErrorAsync(string eventName, string message, Exception exception)
    {
        try
        {
            await logService.WriteAsync(
                new LocalLogEntry(
                    DateTimeOffset.UtcNow,
                    LocalLogLevel.Error,
                    nameof(PdfExportDialogViewModel),
                    eventName,
                    message,
                    exception.ToString()),
                CancellationToken.None).ConfigureAwait(true);
        }
        catch (Exception logException) when (logException is IOException or UnauthorizedAccessException)
        {
        }
    }

    private void RaiseCommandStates()
    {
        RefreshPreviewCommand.RaiseCanExecuteChanged();
        ExportCommand.RaiseCanExecuteChanged();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        lifetimeCancellation.Cancel();
        Preview.Dispose();
        lifetimeCancellation.Dispose();
        try
        {
            if (File.Exists(previewPath))
            {
                File.Delete(previewPath);
            }

            if (Directory.Exists(previewDirectory) && !Directory.EnumerateFileSystemEntries(previewDirectory).Any())
            {
                Directory.Delete(previewDirectory);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
