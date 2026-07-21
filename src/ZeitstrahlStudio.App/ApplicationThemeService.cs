using System.IO;
using System.Windows;
using Microsoft.Win32;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Wendet das projektbezogene Farbschema ohne Neustart auf WPF-Ressourcen an.</summary>
public interface IApplicationThemeService
{
    ApplicationTheme CurrentTheme { get; }
    bool IsDark { get; }
    event EventHandler? ThemeChanged;
    void Apply(ApplicationTheme theme);
}

/// <summary>Lokale WPF-Implementierung mit optionaler Übernahme der Windows-App-Einstellung.</summary>
public sealed class ApplicationThemeService : IApplicationThemeService
{
    public ApplicationTheme CurrentTheme { get; private set; } = ApplicationTheme.FollowWindows;

    public bool IsDark { get; private set; }

    public event EventHandler? ThemeChanged;

    public void Apply(ApplicationTheme theme)
    {
        if (!Enum.IsDefined(theme))
        {
            throw new ArgumentOutOfRangeException(nameof(theme), "Das Farbschema wird nicht unterstützt.");
        }

        var effectiveDark = theme == ApplicationTheme.Dark ||
            theme == ApplicationTheme.FollowWindows && WindowsUsesDarkApps();
        var changed = CurrentTheme != theme || IsDark != effectiveDark;
        CurrentTheme = theme;
        IsDark = effectiveDark;
        ApplyPalette(effectiveDark);
        if (changed)
        {
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static void ApplyPalette(bool dark)
    {
        var application = System.Windows.Application.Current;
        if (application is null)
        {
            return;
        }

        var source = dark
            ? new Uri("Themes/Theme.Dark.xaml", UriKind.Relative)
            : new Uri("Themes/Theme.Light.xaml", UriKind.Relative);
        var themeDictionary = new ResourceDictionary { Source = source };

        var merged = application.Resources.MergedDictionaries;
        for (var index = merged.Count - 1; index >= 0; index--)
        {
            var dictionary = merged[index];
            if (dictionary.Source?.OriginalString.Contains("Theme.", StringComparison.OrdinalIgnoreCase) == true)
            {
                merged.RemoveAt(index);
            }
        }

        merged.Insert(0, themeDictionary);
    }


    private static bool WindowsUsesDarkApps()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                writable: false);
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch (Exception exception) when (
            exception is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            return false;
        }
    }
}
