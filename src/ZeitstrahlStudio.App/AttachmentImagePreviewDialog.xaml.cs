using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ZeitstrahlStudio.Domain;

namespace ZeitstrahlStudio.App;

/// <summary>Dekodiert eine geprüfte Bild-Projektkopie mit begrenzter Vorschauauflösung.</summary>
public partial class AttachmentImagePreviewDialog : Window
{
    private const int MaximumPreviewWidth = 2_400;
    private const long MaximumPreviewFileSize = 512L * 1024 * 1024;

    public AttachmentImagePreviewDialog(Attachment attachment, string validatedLocalPath)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        InitializeComponent();
        if (attachment.FileSize > MaximumPreviewFileSize)
        {
            throw new InvalidDataException(
                "Das Bild ist für die integrierte Vorschau zu groß. Es kann im Standardprogramm geöffnet werden.");
        }

        var image = LoadImage(validatedLocalPath);
        DataContext = new ImagePreviewModel(
            attachment.OriginalFileName,
            $"Vorschau: {image.PixelWidth} × {image.PixelHeight} Pixel",
            image);
    }

    private static BitmapImage LoadImage(string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            128 * 1024,
            FileOptions.SequentialScan);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        image.DecodePixelWidth = MaximumPreviewWidth;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private sealed record ImagePreviewModel(
        string FileName,
        string Dimensions,
        BitmapImage PreviewImage);
}
