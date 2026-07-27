using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ZeitstrahlStudio.App;

/// <summary>Barrierefrei beschriftete, direkt auswählbare Ereignisfarbe.</summary>
public sealed record ColorPaletteOption(string Hex, string Label);

/// <summary>Gemeinsame gut unterscheidbare Palette für Ereignisse.</summary>
public static class EventColorPalette
{
    public static IReadOnlyList<ColorPaletteOption> Options { get; } =
    [
        new("#2563EB", "Blau"),
        new("#0891B2", "Türkis"),
        new("#0D9488", "Petrol"),
        new("#16A34A", "Grün"),
        new("#65A30D", "Limette"),
        new("#CA8A04", "Gelb"),
        new("#EA580C", "Orange"),
        new("#DC2626", "Rot"),
        new("#DB2777", "Pink"),
        new("#9333EA", "Violett"),
        new("#4F46E5", "Indigo"),
        new("#475569", "Schiefergrau"),
    ];
}

/// <summary>Erzeugt eine sichere Vorschau aus einer optional noch unvollständigen Hex-Eingabe.</summary>
public sealed class HexColorBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text &&
            text.Length == 7 &&
            text[0] == '#' &&
            text.AsSpan(1).ToArray().All(Uri.IsHexDigit))
        {
            return new SolidColorBrush(Color.FromRgb(
                byte.Parse(text.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(text.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(text.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
        }

        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
