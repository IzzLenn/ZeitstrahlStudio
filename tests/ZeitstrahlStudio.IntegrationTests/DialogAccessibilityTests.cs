using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
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
