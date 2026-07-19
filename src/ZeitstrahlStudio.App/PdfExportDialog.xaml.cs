using System.Windows;

namespace ZeitstrahlStudio.App;

/// <summary>WPF-Rahmen für die MVVM-gesteuerte PDF-Exportvorschau.</summary>
public partial class PdfExportDialog : Window
{
    private readonly PdfExportDialogViewModel viewModel;

    public PdfExportDialog(PdfExportDialogViewModel viewModel)
    {
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = viewModel;
        viewModel.ExportCompleted += OnExportCompleted;
        Closed += OnClosed;
    }

    private void PreviewViewport_OnSizeChanged(object sender, SizeChangedEventArgs e) =>
        viewModel.Preview.UpdateViewport(e.NewSize.Width, e.NewSize.Height);

    private void OnExportCompleted(object? sender, EventArgs e) => DialogResult = true;

    private void OnClosed(object? sender, EventArgs e)
    {
        Closed -= OnClosed;
        viewModel.ExportCompleted -= OnExportCompleted;
        viewModel.Dispose();
    }
}
