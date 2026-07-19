using System.Windows;

namespace ZeitstrahlStudio.App;

/// <summary>WPF-Fenster für die vollständig lokale PDF-Seitenvorschau.</summary>
public partial class AttachmentPdfPreviewDialog : Window
{
    private readonly PdfPreviewDialogViewModel viewModel;

    public AttachmentPdfPreviewDialog(PdfPreviewDialogViewModel viewModel)
    {
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = viewModel;
        Closed += OnClosed;
    }

    private void PreviewViewport_OnSizeChanged(object sender, SizeChangedEventArgs e) =>
        viewModel.UpdateViewport(e.NewSize.Width, e.NewSize.Height);

    private void OnClosed(object? sender, EventArgs e)
    {
        Closed -= OnClosed;
        viewModel.Dispose();
    }
}
