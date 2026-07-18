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
}
