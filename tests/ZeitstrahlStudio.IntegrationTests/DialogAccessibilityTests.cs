using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ZeitstrahlStudio.App;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.IntegrationTests;

public sealed class DialogAccessibilityTests
{
    [Fact]
    public async Task AttachmentImagePreviewDialog_ExposesItsHeaderViewportAndCloseAction()
    {
        await RunOnStaThread(() =>
        {
            var directory = Path.Combine(Path.GetTempPath(), "ZeitstrahlStudio.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var imagePath = Path.Combine(directory, "vorschau.png");
            try
            {
                WriteTestImage(imagePath);
                var attachment = new Attachment(
                    Guid.NewGuid(),
                    "vorschau.png",
                    "image/png",
                    new FileInfo(imagePath).Length,
                    new string('a', 64),
                    null,
                    new DateTimeOffset(2026, 7, 25, 12, 0, 0, TimeSpan.Zero),
                    "attachments/vorschau.png");
                var dialog = new AttachmentImagePreviewDialog(attachment, imagePath);
                Layout(dialog, 560, 420);

                Assert.Equal("Bildvorschau", dialog.Title);
                Assert.NotNull(dialog.FindName("ImagePreviewHeader"));
                var viewport = Assert.IsType<ScrollViewer>(dialog.FindName("ImagePreviewViewport"));
                Assert.Equal("Bildvorschau", AutomationProperties.GetName(viewport));
                var close = FindButton(dialog, "Bildvorschau schließen");
                Assert.True(close.IsCancel);
                Assert.True(close.IsDefault);
                AssertInside(dialog, viewport);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        });
    }

    [Fact]
    public async Task AuditLogDialog_ExposesEntriesAndAReadableEmptyState()
    {
        await RunOnStaThread(() =>
        {
            var emptyDialog = new AuditLogDialog([]);
            Layout(emptyDialog, 720, 420);
            Assert.Equal("Änderungsprotokoll", emptyDialog.Title);
            var emptyState = Assert.IsType<Border>(emptyDialog.FindName("AuditEmptyState"));
            Assert.Equal(Visibility.Visible, emptyState.Visibility);

            var entry = new AuditEntry(
                Guid.NewGuid(),
                new DateTimeOffset(2026, 7, 25, 12, 0, 0, TimeSpan.Zero),
                "Update",
                "TimelineEvent",
                Guid.NewGuid(),
                new string('B', 2_000),
                true,
                null);
            var populatedDialog = new AuditLogDialog([entry]);
            Layout(populatedDialog, 720, 420);
            var entries = Assert.IsType<DataGrid>(populatedDialog.FindName("AuditEntriesGrid"));
            Assert.Equal("Protokolleinträge", AutomationProperties.GetName(entries));
            Assert.Single(entries.Items);
            Assert.Equal(Visibility.Collapsed, Assert.IsType<Border>(populatedDialog.FindName("AuditEmptyState")).Visibility);
            var close = FindButton(populatedDialog, "Änderungsprotokoll schließen");
            Assert.True(close.IsCancel);
            Assert.True(close.IsDefault);
            AssertInside(populatedDialog, entries);
        });
    }

    [Fact]
    public async Task HtmlExportOptionsDialog_ExposesOptionsAndPrimaryAction()
    {
        await RunOnStaThread(() =>
        {
            var dialog = new HtmlExportOptionsDialog(TimelineOrientation.Vertical);
            Layout(dialog, 520, 420);

            Assert.Equal("Standalone-HTML-Export", dialog.Title);
            var optionsPanel = Assert.IsType<ScrollViewer>(dialog.FindName("HtmlExportOptionsPanel"));
            Assert.Equal("HTML-Exportoptionen", AutomationProperties.GetName(optionsPanel));
            var orientation = Assert.IsType<ComboBox>(dialog.FindName("OrientationBox"));
            Assert.Equal(1, orientation.SelectedIndex);
            var export = FindButton(dialog, "HTML exportieren und Zielpfad auswählen");
            var cancel = FindButton(dialog, "HTML-Export abbrechen");
            Assert.True(export.IsDefault);
            Assert.True(cancel.IsCancel);
            Assert.Equal("HTML exportieren …", export.Content);
            AssertInside(dialog, optionsPanel);
        });
    }

    [Fact]
    public async Task ApplicationSettingsDialog_AndEventColorPaletteAreKeyboardReachable()
    {
        await RunOnStaThread(() =>
        {
            var settings = new ApplicationSettingsDialog(ApplicationTheme.Dark);
            Layout(settings, 520, 330);
            var themeBox = Assert.IsType<ComboBox>(settings.FindName("ApplicationThemeBox"));
            Assert.Equal("Globales Farbschema", AutomationProperties.GetName(themeBox));
            Assert.True(themeBox.IsEnabled);
            Assert.True(FindButton(settings, "Einstellungen übernehmen").IsDefault);

            var editor = new EventEditorDialog(null);
            Layout(editor, 900, 720);
            var palette = Assert.IsType<ListBox>(editor.FindName("EventColorPalette"));
            Assert.Equal("Visuelle Ereignisfarbauswahl", AutomationProperties.GetName(palette));
            Assert.True(palette.Focusable);
            Assert.True(palette.Items.Count >= 12);
        });
    }

    [Fact]
    public async Task ComboBoxes_RenderSelectedLabelsOnDarkClosedAndOpenSurfaces()
    {
        await RunOnStaThread(() =>
        {
            var applicationDialog = new ApplicationSettingsDialog(ApplicationTheme.Dark);
            AddDarkTheme(applicationDialog);
            Layout(applicationDialog, 520, 330);
            var applicationTheme = Assert.IsType<ComboBox>(applicationDialog.FindName("ApplicationThemeBox"));
            AssertDarkClosedComboBox(applicationTheme, "Dunkel");

            applicationTheme.IsDropDownOpen = true;
            applicationTheme.UpdateLayout();
            var popup = Assert.IsType<Popup>(applicationTheme.Template.FindName("PART_Popup", applicationTheme));
            var popupBorder = Assert.IsType<Border>(popup.Child);
            Assert.Equal(Color.FromRgb(0x1E, 0x29, 0x3B), AssertSolidColor(popupBorder.Background));
            Assert.NotEqual(Colors.White, AssertSolidColor(popupBorder.Background));
            popupBorder.Measure(new Size(applicationTheme.ActualWidth, 240));
            popupBorder.Arrange(new Rect(0, 0, applicationTheme.ActualWidth, popupBorder.DesiredSize.Height));
            popupBorder.UpdateLayout();
            var popupLabels = FindVisualDescendants<TextBlock>(popupBorder)
                .Select(text => text.Text)
                .ToArray();
            Assert.Contains("Windows-Einstellung übernehmen", popupLabels);
            Assert.Contains("Hell", popupLabels);
            Assert.Contains("Dunkel", popupLabels);

            var projectDialog = new ProjectSettingsDialog(new ProjectSettingsDialogViewModel(
                new ProjectSettings
                {
                    Theme = ApplicationTheme.Dark,
                    PreferredOrientation = TimelineOrientation.Horizontal,
                }));
            AddDarkTheme(projectDialog);
            Layout(projectDialog, 620, 650);
            AssertDarkClosedComboBox(
                Assert.IsType<ComboBox>(projectDialog.FindName("ProjectThemeBox")),
                "Dunkel");
            AssertDarkClosedComboBox(
                Assert.IsType<ComboBox>(projectDialog.FindName("ProjectOrientationBox")),
                "Horizontal");
        });
    }

    [Theory]
    [InlineData("AttachmentPdfPreviewDialog.xaml", "PDF-Vorschau Werkzeuge")]
    [InlineData("PdfExportDialog.xaml", "PDF-Exportvorschau Werkzeuge")]
    public void PdfDialogXaml_UsesTextToolsAndSemanticDialogResources(string fileName, string toolName)
    {
        var path = Path.Combine(FindAppRoot(), fileName);
        var xaml = File.ReadAllText(path);

        Assert.Contains("{DynamicResource DialogBackgroundBrush}", xaml, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource HeaderBackgroundBrush}", xaml, StringComparison.Ordinal);
        Assert.Contains($"AutomationProperties.Name=\"{toolName}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Vorherige Seite\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Nächste Seite\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Verkleinern\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Vergrößern\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Themes/Theme.Light.xaml", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Content=\"◀\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"▶\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Content=\"−\"", xaml, StringComparison.Ordinal);
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
    private static void WriteTestImage(string path)
    {
        var pixels = new byte[4 * 4 * 4];
        for (var index = 0; index < pixels.Length; index += 4)
        {
            pixels[index] = 0x40;
            pixels[index + 1] = 0x80;
            pixels[index + 2] = 0xC0;
            pixels[index + 3] = 0xFF;
        }

        var bitmap = BitmapSource.Create(4, 4, 96, 96, PixelFormats.Bgra32, null, pixels, 16);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static void AddDarkTheme(FrameworkElement element) =>
        element.Resources.MergedDictionaries.Insert(0, new ResourceDictionary
        {
            Source = new Uri(
                "/ZeitstrahlStudio.App;component/Themes/Theme.Dark.xaml",
                UriKind.Relative),
        });

    private static void AssertDarkClosedComboBox(ComboBox comboBox, string expectedLabel)
    {
        _ = comboBox.ApplyTemplate();
        comboBox.UpdateLayout();

        Assert.Contains(
            FindVisualDescendants<TextBlock>(comboBox),
            text => string.Equals(text.Text, expectedLabel, StringComparison.Ordinal));
        Assert.Equal(Color.FromRgb(0x0F, 0x17, 0x2A), AssertSolidColor(comboBox.Background));
        var fieldBorder = Assert.IsType<Border>(comboBox.Template.FindName("FieldBorder", comboBox));
        Assert.Equal(Color.FromRgb(0x0F, 0x17, 0x2A), AssertSolidColor(fieldBorder.Background));

        var width = Math.Max(1, (int)Math.Ceiling(comboBox.ActualWidth));
        var height = Math.Max(1, (int)Math.Ceiling(comboBox.ActualHeight));
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        var drawing = new DrawingVisual();
        using (var context = drawing.RenderOpen())
        {
            context.DrawRectangle(new VisualBrush(comboBox), null, new Rect(0, 0, width, height));
        }

        bitmap.Render(drawing);
        var pixels = new byte[width * height * 4];
        bitmap.CopyPixels(pixels, width * 4, 0);

        var darkPixels = 0;
        var brightPixels = 0;
        var samplePixels = 0;
        var contentRight = Math.Max(4, width - 32);
        for (var y = 3; y < height - 3; y++)
        {
            for (var x = 3; x < contentRight; x++)
            {
                var offset = ((y * width) + x) * 4;
                var blue = pixels[offset];
                var green = pixels[offset + 1];
                var red = pixels[offset + 2];
                if (red < 120 && green < 120 && blue < 120)
                {
                    darkPixels++;
                }

                if (red > 175 && green > 175 && blue > 175)
                {
                    brightPixels++;
                }

                samplePixels++;
            }
        }

        Assert.True(darkPixels > samplePixels * 0.70, $"Das Feld für '{expectedLabel}' wurde nicht überwiegend dunkel gerendert.");
        var selectedText = FindVisualDescendants<TextBlock>(comboBox).Single(text => text.Text == expectedLabel);
        Assert.True(brightPixels >= 4, $"Der ausgewählte Text '{expectedLabel}' wurde nicht sichtbar gerendert. Helle Pixel: {brightPixels}; Textfarbe: {selectedText.Foreground}.");
        Assert.True(brightPixels < samplePixels * 0.25, $"Das Feld für '{expectedLabel}' enthält weiterhin eine helle Systemfläche.");
    }

    private static Color AssertSolidColor(Brush brush) => Assert.IsType<SolidColorBrush>(brush).Color;

    private static Button FindButton(DependencyObject root, string automationName) =>
        FindDescendants<Button>(root).Single(button =>
            string.Equals(AutomationProperties.GetName(button), automationName, StringComparison.Ordinal));

    private static IEnumerable<T> FindDescendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in FindDescendants<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private static IEnumerable<T> FindVisualDescendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in FindVisualDescendants<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private static void Layout(Window dialog, double width, double height)
    {
        var content = Assert.IsAssignableFrom<FrameworkElement>(dialog.Content);
        content.Measure(new Size(width, height));
        content.Arrange(new Rect(0, 0, width, height));
        content.UpdateLayout();
    }

    private static void AssertInside(Window dialog, FrameworkElement element)
    {
        var content = Assert.IsAssignableFrom<FrameworkElement>(dialog.Content);
        var topLeft = element.TranslatePoint(new Point(0, 0), content);
        Assert.InRange(topLeft.X, -0.1, content.ActualWidth + 0.1);
        Assert.InRange(topLeft.Y, -0.1, content.ActualHeight + 0.1);
        Assert.InRange(topLeft.X + element.ActualWidth, 0, content.ActualWidth + 0.1);
        Assert.InRange(topLeft.Y + element.ActualHeight, 0, content.ActualHeight + 0.1);
    }

    private static Task RunOnStaThread(Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
            finally
            {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return AwaitCompletionAsync(completion.Task, thread);
    }

    private static async Task AwaitCompletionAsync(Task completion, Thread thread)
    {
        await completion.WaitAsync(TimeSpan.FromSeconds(30));
        thread.Join(TimeSpan.FromSeconds(5));
    }
}
