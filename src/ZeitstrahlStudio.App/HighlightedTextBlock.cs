using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ZeitstrahlStudio.App;

/// <summary>Stellt die vom Suchdienst mit ⟦…⟧ markierte Fundstelle visuell hervorgehoben dar.</summary>
public sealed class HighlightedTextBlock : TextBlock
{
    private static readonly Brush HighlightBackground = CreateBrush("#FEF3C7");
    private static readonly Brush HighlightForeground = CreateBrush("#92400E");

    public static readonly DependencyProperty HighlightedTextProperty = DependencyProperty.Register(
        nameof(HighlightedText),
        typeof(string),
        typeof(HighlightedTextBlock),
        new FrameworkPropertyMetadata(string.Empty, OnHighlightedTextChanged));

    public HighlightedTextBlock()
    {
        TextWrapping = TextWrapping.Wrap;
    }

    public string HighlightedText
    {
        get => (string)GetValue(HighlightedTextProperty);
        set => SetValue(HighlightedTextProperty, value);
    }

    private static void OnHighlightedTextChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (HighlightedTextBlock)dependencyObject;
        control.RebuildInlines((string?)e.NewValue ?? string.Empty);
    }

    private void RebuildInlines(string value)
    {
        Inlines.Clear();
        var cursor = 0;
        while (cursor < value.Length)
        {
            var markerStart = value.IndexOf('⟦', cursor);
            if (markerStart < 0)
            {
                Inlines.Add(new Run(value[cursor..]));
                break;
            }

            if (markerStart > cursor)
            {
                Inlines.Add(new Run(value[cursor..markerStart]));
            }

            var markerEnd = value.IndexOf('⟧', markerStart + 1);
            if (markerEnd < 0)
            {
                Inlines.Add(new Run(value[markerStart..]));
                break;
            }

            Inlines.Add(new Run(value[(markerStart + 1)..markerEnd])
            {
                Background = HighlightBackground,
                Foreground = HighlightForeground,
                FontWeight = FontWeights.SemiBold,
            });
            cursor = markerEnd + 1;
        }
    }

    private static Brush CreateBrush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }
}
