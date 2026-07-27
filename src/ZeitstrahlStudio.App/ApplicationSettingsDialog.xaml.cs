using System.Windows;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Verwaltet ausschließlich den modalen Lebenszyklus der globalen Einstellungen.</summary>
public partial class ApplicationSettingsDialog : Window
{
    private readonly ApplicationSettingsDialogViewModel viewModel;

    public ApplicationSettingsDialog(ApplicationTheme currentTheme)
    {
        viewModel = new ApplicationSettingsDialogViewModel(currentTheme);
        InitializeComponent();
        DataContext = viewModel;
    }

    public ApplicationTheme Result => viewModel.Result;

    private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
