using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Bindbares Formular für globale, projektunabhängige Anwendungseinstellungen.</summary>
public sealed class ApplicationSettingsDialogViewModel : ObservableObject
{
    private SelectionOption<ApplicationTheme> selectedTheme;

    public ApplicationSettingsDialogViewModel(ApplicationTheme currentTheme)
    {
        ThemeOptions =
        [
            new(ApplicationTheme.FollowWindows, "Windows-Einstellung übernehmen"),
            new(ApplicationTheme.Light, "Hell"),
            new(ApplicationTheme.Dark, "Dunkel"),
        ];
        selectedTheme = ThemeOptions.Single(option => option.Value == currentTheme);
    }

    public IReadOnlyList<SelectionOption<ApplicationTheme>> ThemeOptions { get; }

    public SelectionOption<ApplicationTheme> SelectedTheme
    {
        get => selectedTheme;
        set => SetProperty(ref selectedTheme, value ?? throw new ArgumentNullException(nameof(value)));
    }

    public ApplicationTheme Result => SelectedTheme.Value;
}
