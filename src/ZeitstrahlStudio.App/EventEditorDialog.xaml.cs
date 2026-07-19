using System.Windows;
using ZeitstrahlStudio.Application;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Dialoglebenszyklus für das gebundene Ereignisformular.</summary>
public partial class EventEditorDialog : Window
{
    private readonly EventEditorDialogViewModel viewModel;

    public EventEditorDialog(
        TimelineEvent? timelineEvent,
        string defaultEventColorHex = "#3B82F6")
    {
        viewModel = new EventEditorDialogViewModel(timelineEvent, defaultEventColorHex);
        InitializeComponent();
        DataContext = viewModel;
    }

    public EventEditRequest? Result { get; private set; }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (!viewModel.TryBuildRequest(out var request, out var errorMessage))
        {
            MessageBox.Show(
                this,
                errorMessage,
                "Eingabe prüfen",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        Result = request;
        DialogResult = true;
    }
}
