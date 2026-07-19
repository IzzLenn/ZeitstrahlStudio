using System.Windows;

namespace ZeitstrahlStudio.App;

/// <summary>Beschränkt den Code-behind auf Validierungsaufruf und modalen Fensterabschluss.</summary>
public partial class ProjectSettingsDialog : Window
{
    private readonly ProjectSettingsDialogViewModel viewModel;

    public ProjectSettingsDialog(ProjectSettingsDialogViewModel viewModel)
    {
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (viewModel.TryBuildResult())
        {
            DialogResult = true;
        }
    }
}
