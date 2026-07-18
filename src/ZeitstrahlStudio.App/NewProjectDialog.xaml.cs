using System.Windows;

namespace ZeitstrahlStudio.App;

/// <summary>Rein UI-bezogener Dialog zur Eingabe eines Projektnamens.</summary>
public partial class NewProjectDialog : Window
{
    public NewProjectDialog()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ProjectNameTextBox.Focus();
            ProjectNameTextBox.SelectAll();
        };
    }

    public string ProjectName => ProjectNameTextBox.Text.Trim();

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            MessageBox.Show(this, "Bitte geben Sie einen Projektnamen ein.", "Neues Projekt");
            return;
        }

        DialogResult = true;
    }
}
