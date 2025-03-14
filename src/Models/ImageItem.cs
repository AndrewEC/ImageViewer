namespace ImageViewer.Models;

using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

/// <summary>
/// Represents the details of a sigle image within a selected sub-folder.
/// </summary>
/// <param name="absolutePath">The absolute path to the image on disk.</param>
public sealed class ImageItem(string absolutePath)
{
    /// <summary>
    /// Gets the absolute path to the image on disk.
    /// </summary>
    public string AbsolutePath { get; } = absolutePath;

    /// <summary>
    /// Gets the image bitmap. This should only be used when viewing the full image.
    /// When viewing the image as part of a list or grid the more memory efficient
    /// <see cref="Thumbnail"/> property should be preferred instead.
    /// </summary>
    public Task<Bitmap> Image { get; }
        = Task.Run(() => new Bitmap(absolutePath));

    /// <summary>
    /// Gets a thumbnail of the image. Sized to be no-more than 200 pixels in width.
    /// </summary>
    public Task<Bitmap> Thumbnail { get; }
        = Task.Run(() =>
        {
            MemoryStream stream = new(File.ReadAllBytes(absolutePath));
            return Bitmap.DecodeToWidth(stream, 200, BitmapInterpolationMode.LowQuality);
        });
}