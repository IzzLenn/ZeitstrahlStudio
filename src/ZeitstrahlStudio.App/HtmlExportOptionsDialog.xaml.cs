using System.Windows;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Erfasst ausschließlich die sichtbaren Optionen des Standalone-HTML-Exports.</summary>
public partial class HtmlExportOptionsDialog : Window
{
    public HtmlExportOptionsDialog(TimelineOrientation preferredOrientation)
    {
        InitializeComponent();
        OrientationBox.SelectedIndex = preferredOrientation == TimelineOrientation.Vertical ? 1 : 0;
    }

    public HtmlExportOptions? Options { get; private set; }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        Options = new HtmlExportOptions(
            OrientationBox.SelectedIndex == 1
                ? TimelineOrientation.Vertical
                : TimelineOrientation.Horizontal,
            IncludeThumbnailsBox.IsChecked == true,
            IncludeNotesBox.IsChecked == true,
            ShowSnapshotBannerBox.IsChecked == true,
            IncludeDocumentCopiesBox.IsChecked == true);
        DialogResult = true;
    }
}
