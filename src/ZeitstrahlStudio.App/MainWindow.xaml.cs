using System.ComponentModel;
using System.Windows;

namespace ZeitstrahlStudio.App;

/// <summary>Hauptfenster; Code-behind behandelt ausschließlich den Fensterlebenszyklus.</summary>
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel;
    private bool closeApproved;
    private bool closeInProgress;

    public MainWindow(MainWindowViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (closeApproved)
        {
            return;
        }

        e.Cancel = true;
        if (closeInProgress)
        {
            return;
        }

        closeInProgress = true;
        try
        {
            if (await viewModel.PrepareToCloseAsync().ConfigureAwait(true))
            {
                closeApproved = true;
                Close();
            }
        }
        finally
        {
            closeInProgress = false;
        }
    }

    private void Window_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) && viewModel.CanAcceptDroppedFiles
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (!viewModel.CanAcceptDroppedFiles ||
            e.Data.GetData(DataFormats.FileDrop) is not string[] paths ||
            paths.Length == 0)
        {
            return;
        }

        await viewModel.ImportDroppedFilesAsync(paths).ConfigureAwait(true);
    }
}
