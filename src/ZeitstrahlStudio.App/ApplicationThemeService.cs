using System.IO;
using System.Windows.Media;
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

        SetBrush(application, "WindowBackgroundBrush", dark ? "#0F172A" : "#F1F5F9");
        SetBrush(application, "SurfaceBrush", dark ? "#111827" : "#F8FAFC");
        SetBrush(application, "RaisedSurfaceBrush", dark ? "#1E293B" : "#FFFFFF");
        SetBrush(application, "SurfaceAltBrush", dark ? "#334155" : "#E2E8F0");
        SetBrush(application, "InputBackgroundBrush", dark ? "#0F172A" : "#FFFFFF");
        SetBrush(application, "PrimaryTextBrush", dark ? "#F8FAFC" : "#0F172A");
        SetBrush(application, "SecondaryTextBrush", dark ? "#E2E8F0" : "#334155");
        SetBrush(application, "MutedTextBrush", dark ? "#CBD5E1" : "#64748B");
        SetBrush(application, "SubtleTextBrush", dark ? "#CBD5E1" : "#475569");
        SetBrush(application, "BorderBrush", dark ? "#475569" : "#CBD5E1");
        SetBrush(application, "StrongBorderBrush", dark ? "#64748B" : "#94A3B8");
        SetBrush(application, "HeaderBackgroundBrush", dark ? "#020617" : "#0F172A");
        SetBrush(application, "HeaderForegroundBrush", "#FFFFFF");
        SetBrush(application, "HeaderMutedBrush", "#94A3B8");
        SetBrush(application, "AccentBrush", dark ? "#3B82F6" : "#2563EB");
        SetBrush(application, "AccentDarkBrush", dark ? "#2563EB" : "#1D4ED8");
        SetBrush(application, "AccentTextBrush", dark ? "#60A5FA" : "#1D4ED8");
        SetBrush(application, "ToolbarSeparatorBrush", dark ? "#475569" : "#475569");
    }

    private static void SetBrush(
        System.Windows.Application application,
        string key,
        string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        application.Resources[key] = brush;
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
