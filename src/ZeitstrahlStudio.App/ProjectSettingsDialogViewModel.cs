using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Bindbares und vollständig validierbares Formular für projektbezogene Darstellungswerte.</summary>
public sealed class ProjectSettingsDialogViewModel : ObservableObject
{
    private readonly ProjectSettings original;
    private SelectionOption<ApplicationTheme> selectedTheme;
    private SelectionOption<TimelineOrientation> selectedOrientation;
    private string defaultEventColorHex;
    private double timelineCardFontSize;
    private double timelineAxisFontSize;
    private double exportFontSize;
    private bool compressLargeGaps;
    private string? errorMessage;

    public ProjectSettingsDialogViewModel(ProjectSettings settings)
    {
        original = settings ?? throw new ArgumentNullException(nameof(settings));
        ThemeOptions =
        [
            new(ApplicationTheme.FollowWindows, "Windows-Einstellung übernehmen"),
            new(ApplicationTheme.Light, "Hell"),
            new(ApplicationTheme.Dark, "Dunkel"),
        ];
        OrientationOptions =
        [
            new(TimelineOrientation.Horizontal, "Horizontal"),
            new(TimelineOrientation.Vertical, "Vertikal"),
        ];
        selectedTheme = ThemeOptions.Single(item => item.Value == settings.Theme);
        selectedOrientation = OrientationOptions.Single(item => item.Value == settings.PreferredOrientation);
        defaultEventColorHex = settings.DefaultEventColorHex;
        timelineCardFontSize = settings.TimelineCardFontSize;
        timelineAxisFontSize = settings.TimelineAxisFontSize;
        exportFontSize = settings.ExportFontSize;
        compressLargeGaps = settings.CompressLargeGaps;
    }

    public IReadOnlyList<SelectionOption<ApplicationTheme>> ThemeOptions { get; }

    public IReadOnlyList<SelectionOption<TimelineOrientation>> OrientationOptions { get; }

    public IReadOnlyList<ColorPaletteOption> ColorOptions => EventColorPalette.Options;

    public SelectionOption<ApplicationTheme> SelectedTheme
    {
        get => selectedTheme;
        set => SetProperty(ref selectedTheme, value ?? throw new ArgumentNullException(nameof(value)));
    }

    public SelectionOption<TimelineOrientation> SelectedOrientation
    {
        get => selectedOrientation;
        set => SetProperty(ref selectedOrientation, value ?? throw new ArgumentNullException(nameof(value)));
    }

    public string DefaultEventColorHex
    {
        get => defaultEventColorHex;
        set
        {
            if (SetProperty(ref defaultEventColorHex, value))
            {
                OnPropertyChanged(nameof(SelectedPaletteColorHex));
            }
        }
    }

    public string? SelectedPaletteColorHex
    {
        get => ColorOptions.FirstOrDefault(option =>
            string.Equals(option.Hex, DefaultEventColorHex, StringComparison.OrdinalIgnoreCase))?.Hex;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                DefaultEventColorHex = value;
            }
        }
    }

    public double TimelineCardFontSize
    {
        get => timelineCardFontSize;
        set => SetProperty(ref timelineCardFontSize, value);
    }

    public double TimelineAxisFontSize
    {
        get => timelineAxisFontSize;
        set => SetProperty(ref timelineAxisFontSize, value);
    }

    public double ExportFontSize
    {
        get => exportFontSize;
        set => SetProperty(ref exportFontSize, value);
    }

    public bool CompressLargeGaps
    {
        get => compressLargeGaps;
        set => SetProperty(ref compressLargeGaps, value);
    }

    public string? ErrorMessage
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

    public ProjectSettings? Result { get; private set; }

    public bool TryBuildResult()
    {
        try
        {
            var result = original with
            {
                Theme = SelectedTheme.Value,
                PreferredOrientation = SelectedOrientation.Value,
                DefaultEventColorHex = DefaultEventColorHex.Trim(),
                TimelineCardFontSize = TimelineCardFontSize,
                TimelineAxisFontSize = TimelineAxisFontSize,
                ExportFontSize = ExportFontSize,
                CompressLargeGaps = CompressLargeGaps,
            };
            result.Validate();
            Result = result;
            ErrorMessage = null;
            return true;
        }
        catch (Exception exception) when (
            exception is DomainValidationException or ArgumentException)
        {
            Result = null;
            ErrorMessage = exception.Message;
            return false;
        }
    }
}
