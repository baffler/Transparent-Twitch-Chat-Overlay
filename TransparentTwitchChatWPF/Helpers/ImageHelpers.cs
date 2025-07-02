using System.Windows.Media.Imaging;

namespace TransparentTwitchChatWPF.Helpers;

public static class ImageHelpers
{
    // static "factory" method that creates a new BitmapImage.
    public static BitmapImage LoadFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(url, UriKind.Absolute);
        bitmap.EndInit();

        return bitmap;
    }
}
