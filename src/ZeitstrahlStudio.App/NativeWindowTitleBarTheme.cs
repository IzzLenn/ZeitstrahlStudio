using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace ZeitstrahlStudio.App;

/// <summary>
/// Synchronisiert die native Windows-Titelleiste aller WPF-Fenster mit dem
/// effektiven Anwendungsfarbschema.
/// </summary>
internal static class NativeWindowTitleBarTheme
{
    internal const int UseImmersiveDarkModeAttribute = 20;
    internal const int UseImmersiveDarkModeLegacyAttribute = 19;
    internal const int CaptionColorAttribute = 35;
    internal const int TextColorAttribute = 36;
    internal const uint DefaultDwmColor = 0xFFFFFFFF;

    private static readonly IDwmWindowAttributeApi DwmApi = new DwmWindowAttributeApi();
    private static volatile bool isDark;

    static NativeWindowTitleBarTheme()
    {
        EventManager.RegisterClassHandler(
            typeof(Window),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnWindowLoaded),
            handledEventsToo: true);
    }

    internal static bool IsDark => isDark;

    /// <summary>
    /// Merkt das effektive Theme für zukünftige Fenster und aktualisiert alle
    /// bereits erzeugten Anwendungsfenster.
    /// </summary>
    internal static void ApplyToApplicationWindows(bool dark)
    {
        isDark = dark;

        var application = System.Windows.Application.Current;
        if (application is null)
        {
            return;
        }

        if (!application.Dispatcher.CheckAccess())
        {
            application.Dispatcher.Invoke(() => ApplyToApplicationWindows(dark));
            return;
        }

        foreach (Window window in application.Windows)
        {
            ApplyToWindow(window, dark);
        }
    }

    /// <summary>
    /// Führt die DWM-Attributfolge aus. Diese Methode ist separat testbar und
    /// benötigt weder ein sichtbares Fenster noch Pixelvergleiche.
    /// </summary>
    internal static void ApplyAttributes(
        nint windowHandle,
        bool dark,
        bool supportsExplicitColors,
        uint captionColor,
        uint textColor,
        IDwmWindowAttributeApi api)
    {
        ArgumentNullException.ThrowIfNull(api);

        var immersiveValue = dark ? 1 : 0;
        var result = api.SetInt32(windowHandle, UseImmersiveDarkModeAttribute, immersiveValue);
        if (result < 0)
        {
            api.SetInt32(windowHandle, UseImmersiveDarkModeLegacyAttribute, immersiveValue);
        }

        if (!supportsExplicitColors)
        {
            return;
        }

        api.SetUInt32(
            windowHandle,
            CaptionColorAttribute,
            dark ? captionColor : DefaultDwmColor);
        api.SetUInt32(
            windowHandle,
            TextColorAttribute,
            dark ? textColor : DefaultDwmColor);
    }

    internal static uint ToColorRef(Color color) =>
        color.R | ((uint)color.G << 8) | ((uint)color.B << 16);

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window)
        {
            ApplyToWindow(window, isDark);
        }
    }

    private static void ApplyToWindow(Window window, bool dark)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == nint.Zero)
        {
            return;
        }

        var captionColor = GetResourceColor(
            "WindowBackgroundBrush",
            Color.FromRgb(0x0F, 0x17, 0x2A));
        var textColor = GetResourceColor(
            "PrimaryTextBrush",
            Color.FromRgb(0xF8, 0xFA, 0xFC));

        try
        {
            ApplyAttributes(
                handle,
                dark,
                OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000),
                ToColorRef(captionColor),
                ToColorRef(textColor),
                DwmApi);
        }
        catch (Exception exception) when (
            exception is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            // Die native Titelleiste ist eine progressive Darstellungshilfe.
            // Ein fehlendes DWM-Attribut darf das Fenster niemals verhindern.
        }
    }

    private static Color GetResourceColor(string key, Color fallback)
    {
        var resource = System.Windows.Application.Current?.TryFindResource(key);
        return resource is SolidColorBrush brush ? brush.Color : fallback;
    }
}

internal interface IDwmWindowAttributeApi
{
    int SetInt32(nint windowHandle, int attribute, int value);

    int SetUInt32(nint windowHandle, int attribute, uint value);
}

internal sealed class DwmWindowAttributeApi : IDwmWindowAttributeApi
{
    public int SetInt32(nint windowHandle, int attribute, int value) =>
        DwmSetWindowAttributeInt32(windowHandle, attribute, ref value, sizeof(int));

    public int SetUInt32(nint windowHandle, int attribute, uint value) =>
        DwmSetWindowAttributeUInt32(windowHandle, attribute, ref value, sizeof(uint));

    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", PreserveSig = true)]
    private static extern int DwmSetWindowAttributeInt32(
        nint windowHandle,
        int attribute,
        ref int value,
        int valueSize);

    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", PreserveSig = true)]
    private static extern int DwmSetWindowAttributeUInt32(
        nint windowHandle,
        int attribute,
        ref uint value,
        int valueSize);
}