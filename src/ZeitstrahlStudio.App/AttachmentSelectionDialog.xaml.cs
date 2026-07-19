using System.Windows;
using System.Windows.Input;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Wiederverwendbarer Auswahldialog für einen Ereignisanhang.</summary>
public partial class AttachmentSelectionDialog : Window
{
    public AttachmentSelectionDialog(
        IReadOnlyCollection<Attachment> attachments,
        string title,
        string heading,
        string helpText,
        string actionText,
        bool isDestructive)
    {
        InitializeComponent();
        DataContext = attachments ?? throw new ArgumentNullException(nameof(attachments));
        Title = title;
        HeadingText.Text = heading;
        HelpText.Text = helpText;
        ConfirmButton.Content = actionText;
        ConfirmButton.Background = isDestructive
            ? System.Windows.Media.Brushes.Firebrick
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 99, 235));
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
                Title,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        Result = attachment;
        DialogResult = true;
    }
}
