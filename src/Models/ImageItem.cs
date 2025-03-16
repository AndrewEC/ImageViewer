namespace ImageViewer.Models;

using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using ImageViewer.Util;

/// <summary>
/// Represents the details of a sigle image within a selected sub-folder.
/// </summary>
/// <param name="absolutePath">The absolute path to the image on disk.</param>
public sealed class ImageItem(string absolutePath) : IFindable
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
    public Task<Bitmap> Thumbnail { get; } = ThumbnailCache.Instance.LoadThumbnail(absolutePath);

    /// <summary>
    /// Stringifies this image item instance.
    /// </summary>
    /// <returns>The string representation of this image item.</returns>
    public override string ToString() => string.Format($"ImageItem(AbsolutePath={AbsolutePath})");
}