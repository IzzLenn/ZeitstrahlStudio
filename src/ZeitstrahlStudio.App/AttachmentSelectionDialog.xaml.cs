using System.Windows;
using System.Windows.Input;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Auswahldialog für die sichere Entfernung einer Anhangszuordnung.</summary>
public partial class AttachmentSelectionDialog : Window
{
    public AttachmentSelectionDialog(IReadOnlyCollection<Attachment> attachments)
    {
        InitializeComponent();
        DataContext = attachments ?? throw new ArgumentNullException(nameof(attachments));
    }

    public Attachment? Result { get; private set; }

    private void Confirm_Click(object sender, RoutedEventArgs e) => ConfirmSelection();

    private void AttachmentList_MouseDoubleClick(object sender, MouseButtonEventArgs e) =>
        ConfirmSelection();

    private void ConfirmSelection()
    {
        if (AttachmentList.SelectedItem is not Attachment attachment)
        {
            MessageBox.Show(
                this,
                "Bitte wählen Sie einen Anhang aus.",
                "Anhang entfernen",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        Result = attachment;
        DialogResult = true;
    }
}
