using System.Globalization;
using System.Xml.Linq;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class ThemeResourceTests
{
    private static readonly string[] RequiredSemanticBrushes =
    [
        "NavigationBackgroundBrush",
        "NavigationForegroundBrush",
        "NavigationSelectedBackgroundBrush",
        "NavigationSelectedForegroundBrush",
        "CommandBarBackgroundBrush",
        "CommandGroupBackgroundBrush",
        "WorkspaceBackgroundBrush",
        "CardSurfaceBrush",
        "ElevatedSurfaceBrush",
        "InspectorBackgroundBrush",
        "DialogBackgroundBrush",
        "MenuBackgroundBrush",
        "MenuForegroundBrush",
        "ReadOnlyBackgroundBrush",
        "ReadOnlyForegroundBrush",
        "PressedBackgroundBrush",
        "SelectedForegroundBrush",
        "InvalidBackgroundBrush",
        "InvalidBorderBrush",
        "ToolTipBackgroundBrush",
        "ToolTipForegroundBrush",
    ];

    [Fact]
    public void LightAndDarkThemes_ExposeTheSameSemanticBrushContract()
    {
        var appRoot = FindAppRoot();
        var light = ReadBrushes(Path.Combine(appRoot, "Themes", "Theme.Light.xaml"));
        var dark = ReadBrushes(Path.Combine(appRoot, "Themes", "Theme.Dark.xaml"));

        Assert.Equal(light.Keys.Order(StringComparer.Ordinal), dark.Keys.Order(StringComparer.Ordinal));
        foreach (var key in RequiredSemanticBrushes)
        {
            Assert.True(light.ContainsKey(key), $"Im hellen Theme fehlt {key}.");
            Assert.True(dark.ContainsKey(key), $"Im dunklen Theme fehlt {key}.");
        }
    }

    [Theory]
    [InlineData("Theme.Light.xaml")]
    [InlineData("Theme.Dark.xaml")]
    public void Theme_ProvidesReadableCriticalForegroundBackgroundPairs(string themeFile)
    {
        var brushes = ReadBrushes(Path.Combine(FindAppRoot(), "Themes", themeFile));
        var pairs = new[]
        {
            ("PrimaryTextBrush", "WorkspaceBackgroundBrush"),
            ("NavigationForegroundBrush", "NavigationBackgroundBrush"),
            ("MenuForegroundBrush", "MenuBackgroundBrush"),
            ("PrimaryTextBrush", "InspectorBackgroundBrush"),
            ("ReadOnlyForegroundBrush", "ReadOnlyBackgroundBrush"),
            ("ToolTipForegroundBrush", "ToolTipBackgroundBrush"),
        };

        foreach (var (foreground, background) in pairs)
        {
            var ratio = ContrastRatio(brushes[foreground], brushes[background]);
            Assert.True(
                ratio >= 4.5,
                $"{themeFile}: {foreground} auf {background} erreicht nur {ratio:F2}:1.");
        }
    }

    [Fact]
    public void WindowXaml_DoesNotPinALightThemeOutsideApplicationResources()
    {
        var appRoot = FindAppRoot();
        var windowFiles = Directory.EnumerateFiles(appRoot, "*.xaml", SearchOption.TopDirectoryOnly)
            .Where(path => !string.Equals(Path.GetFileName(path), "App.xaml", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.NotEmpty(windowFiles);
        foreach (var path in windowFiles)
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("Themes/Theme.Light.xaml", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Themes/Theme.Dark.xaml", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Typography_UsesTheDocumentedReadableScale()
    {
        var document = XDocument.Load(Path.Combine(FindAppRoot(), "Themes", "Typography.xaml"));
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        var values = document.Root!.Elements()
            .Where(element => element.Name.LocalName == "Double")
            .ToDictionary(
                element => element.Attribute(xaml + "Key")!.Value,
                element => double.Parse(element.Value, CultureInfo.InvariantCulture),
                StringComparer.Ordinal);

        Assert.Equal(12, values["FontSizeXs"]);
        Assert.Equal(12, values["FontSizeSm"]);
        Assert.Equal(13, values["FontSizeBase"]);
        Assert.Equal(14, values["FontSizeMd"]);
        Assert.Equal(16, values["FontSizeLg"]);
        Assert.Equal(20, values["FontSize2Xl"]);
        Assert.Equal(24, values["FontSize3Xl"]);
    }

    [Fact]
    public void SharedControlStyles_PreserveDropdownBindingsWithThemeAwareSurfaces()
    {
        var path = Path.Combine(FindAppRoot(), "Themes", "ControlStyles.xaml");
        var xaml = File.ReadAllText(path);

        Assert.Contains("<Style TargetType=\"ComboBox\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<ControlTemplate TargetType=\"ComboBox\">", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"{TemplateBinding SelectionBoxItem}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ContentTemplate=\"{TemplateBinding SelectionBoxItemTemplate}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ContentTemplateSelector=\"{TemplateBinding ItemTemplateSelector}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ContentStringFormat=\"{TemplateBinding SelectionBoxItemStringFormat}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("TargetType=\"ComboBoxItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ContentSource=\"Content\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"PART_Popup\"", xaml, StringComparison.Ordinal);
        Assert.Contains("TargetType=\"CalendarItem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("TargetType=\"ContextMenu\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Background=\"White\"", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Background=\"#FFFFFF\"", xaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SharedControlStyles_RenderTabsAndDatePickersWithoutNativeWhiteSurfaces()
    {
        var path = Path.Combine(FindAppRoot(), "Themes", "ControlStyles.xaml");
        var xaml = File.ReadAllText(path);

        Assert.Contains("<ControlTemplate TargetType=\"TabItem\">", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"HeaderBorder\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Value=\"{DynamicResource NavigationSelectedBackgroundBrush}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ControlTemplate TargetType=\"DatePicker\">", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DateFieldBorder\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"PART_TextBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"PART_Button\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"PART_Popup\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsOpen=\"{TemplateBinding IsDropDownOpen}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ControlTemplate TargetType=\"DatePickerTextBox\">", xaml, StringComparison.Ordinal);
        Assert.Contains("CalendarStyle\" Value=\"{DynamicResource ThemedCalendarStyle}\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Background=\"White\"", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Background=\"#FFFFFF\"", xaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DarkTheme_OverridesNativeSelectionSurfacesWithoutReplacingControlTemplates()
    {
        var brushes = ReadBrushes(Path.Combine(FindAppRoot(), "Themes", "Theme.Dark.xaml"));

        foreach (var key in new[]
                 {
                     "{x:Static SystemColors.WindowBrushKey}",
                     "{x:Static SystemColors.ControlBrushKey}",
                     "{x:Static SystemColors.HighlightBrushKey}",
                     "{x:Static SystemColors.MenuBrushKey}",
                 })
        {
            Assert.True(brushes.TryGetValue(key, out var brush), $"Im dunklen Theme fehlt {key}.");
            Assert.NotEqual(new RgbColor(255, 255, 255), brush);
        }
    }

    private static Dictionary<string, RgbColor> ReadBrushes(string path)
    {
        var document = XDocument.Load(path);
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        return document.Root!.Elements()
            .Where(element => element.Name.LocalName == "SolidColorBrush")
            .ToDictionary(
                element => element.Attribute(xaml + "Key")!.Value,
                element => ParseColor(element.Attribute("Color")!.Value),
                StringComparer.Ordinal);
    }

    private static RgbColor ParseColor(string value)
    {
        var hex = value.AsSpan().TrimStart('#');
        if (hex.Length == 8)
        {
            hex = hex[2..];
        }

        if (hex.Length != 6)
        {
            throw new InvalidDataException($"Nicht unterstützte Farbe: {value}");
        }

        return new RgbColor(
            byte.Parse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(hex.Slice(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(hex.Slice(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    private static double ContrastRatio(RgbColor foreground, RgbColor background)
    {
        var lighter = Math.Max(Luminance(foreground), Luminance(background));
        var darker = Math.Min(Luminance(foreground), Luminance(background));
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double Luminance(RgbColor color) =>
        (0.2126 * Linearize(color.Red)) +
        (0.7152 * Linearize(color.Green)) +
        (0.0722 * Linearize(color.Blue));

    private static double Linearize(byte component)
    {
        var value = component / 255d;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    private static string FindAppRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ZeitstrahlStudio.sln")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new DirectoryNotFoundException("Der Repository-Stamm konnte nicht ermittelt werden.");
        }

        return Path.Combine(directory.FullName, "src", "ZeitstrahlStudio.App");
    }

    private readonly record struct RgbColor(byte Red, byte Green, byte Blue);
}
