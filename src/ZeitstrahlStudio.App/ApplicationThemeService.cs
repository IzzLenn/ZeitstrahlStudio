using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Lädt, speichert und wendet das globale Farbschema ohne Neustart an.</summary>
public interface IApplicationThemeService
{
    ApplicationTheme CurrentTheme { get; }
    bool IsDark { get; }
    event EventHandler? ThemeChanged;
    Task InitializeAsync(CancellationToken cancellationToken);
    void Apply(ApplicationTheme theme);
    Task ApplyAndSaveAsync(ApplicationTheme theme, CancellationToken cancellationToken);
}

/// <summary>Lokale WPF-Implementierung mit atomarer Persistenz und optionaler Windows-Übernahme.</summary>
public sealed class ApplicationThemeService : IApplicationThemeService, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private readonly string settingsFilePath;
    private readonly SemaphoreSlim gate = new(1, 1);

    public ApplicationThemeService(string? settingsFilePath = null)
    {
        this.settingsFilePath = Path.GetFullPath(settingsFilePath ?? GetDefaultSettingsFilePath());
    }

    public ApplicationTheme CurrentTheme { get; private set; } = ApplicationTheme.FollowWindows;

    public bool IsDark { get; private set; }

    public event EventHandler? ThemeChanged;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var theme = await ReadThemeAsync(cancellationToken).ConfigureAwait(false);
            await ApplyOnApplicationDispatcherAsync(theme);
        }
        finally
        {
            gate.Release();
        }
    }

    public void Apply(ApplicationTheme theme)
    {
        ValidateTheme(theme);

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

    public async Task ApplyAndSaveAsync(ApplicationTheme theme, CancellationToken cancellationToken)
    {
        ValidateTheme(theme);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await WriteThemeAsync(theme, cancellationToken).ConfigureAwait(false);
            await ApplyOnApplicationDispatcherAsync(theme);
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose() => gate.Dispose();

    private Task ApplyOnApplicationDispatcherAsync(ApplicationTheme theme)
    {
        var application = System.Windows.Application.Current;
        if (application is null)
        {
            Apply(theme);
            return Task.CompletedTask;
        }

        return application.Dispatcher.InvokeAsync(() => Apply(theme)).Task;
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

    private async Task<ApplicationTheme> ReadThemeAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(settingsFilePath))
        {
            return ApplicationTheme.FollowWindows;
        }

        try
        {
            await using var stream = new FileStream(
                settingsFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                16 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var state = await JsonSerializer.DeserializeAsync<AppearanceState>(
                stream,
                JsonOptions,
                cancellationToken).ConfigureAwait(false);
            return state is { Version: 1 } && Enum.IsDefined(state.Theme)
                ? state.Theme
                : ApplicationTheme.FollowWindows;
        }
        catch (Exception exception) when (
            exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return ApplicationTheme.FollowWindows;
        }
    }

    private async Task WriteThemeAsync(ApplicationTheme theme, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(settingsFilePath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = settingsFilePath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                16 * 1024,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    new AppearanceState(1, theme),
                    JsonOptions,
                    cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(settingsFilePath))
            {
                File.Replace(temporaryPath, settingsFilePath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, settingsFilePath);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void ValidateTheme(ApplicationTheme theme)
    {
        if (!Enum.IsDefined(theme))
        {
            throw new ArgumentOutOfRangeException(nameof(theme), "Das Farbschema wird nicht unterstützt.");
        }
    }

    private static string GetDefaultSettingsFilePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Zeitstrahl Studio",
        "appearance-settings.json");

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

    private sealed record AppearanceState(int Version, ApplicationTheme Theme);
}
