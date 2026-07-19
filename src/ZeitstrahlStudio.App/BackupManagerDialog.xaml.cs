using System.ComponentModel;
using System.Windows;

namespace ZeitstrahlStudio.App;

/// <summary>WPF-Rahmen für die vollständig MVVM-gesteuerte Sicherungsverwaltung.</summary>
public partial class BackupManagerDialog : Window
{
    private readonly BackupManagerDialogViewModel viewModel;
    private bool allowBusyClose;

    public BackupManagerDialog(BackupManagerDialogViewModel viewModel)
    {
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += OnRequestClose;
        Closed += OnClosed;
    }

    private void OnRequestClose(object? sender, EventArgs e)
    {
        allowBusyClose = true;
        DialogResult = true;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (viewModel.IsBusy && !allowBusyClose)
        {
            e.Cancel = true;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Closed -= OnClosed;
        viewModel.RequestClose -= OnRequestClose;
    }
}
