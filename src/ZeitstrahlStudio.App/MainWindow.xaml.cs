using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace ZeitstrahlStudio.App;

/// <summary>Hauptfenster; Code-behind behandelt ausschließlich Fenster- und Darstellungsinteraktionen.</summary>
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

    private void TimelineZoomOut_Click(object sender, RoutedEventArgs e) => TimelineControl.ZoomOut();

    private void TimelineZoomIn_Click(object sender, RoutedEventArgs e) => TimelineControl.ZoomIn();

    private void TimelineShowAll_Click(object sender, RoutedEventArgs e) => TimelineControl.ShowWholeProject();

    private void TimelineCenterSelection_Click(object sender, RoutedEventArgs e) =>
        TimelineControl.CenterSelectedEvent();

    private void TimelineResetView_Click(object sender, RoutedEventArgs e) => TimelineControl.ResetView();

    private void SearchFocus_Click(object sender, RoutedEventArgs e) => FocusSearch();

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            FocusSearch();
            e.Handled = true;
        }
    }

    private void FocusSearch()
    {
        SearchExpander.IsExpanded = true;
        SearchQueryTextBox.Focus();
        SearchQueryTextBox.SelectAll();
    }
}
