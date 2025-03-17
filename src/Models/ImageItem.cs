namespace ImageViewer.Models;

using System.Text;
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
    public Task<Bitmap?> Image { get; } = ThumbnailCache.Instance.LoadImage(absolutePath);

    /// <summary>
    /// Gets a thumbnail of the image. Sized to be no-more than 200 pixels in width.
    /// </summary>
    public Task<Bitmap?> Thumbnail { get; } = ThumbnailCache.Instance.LoadThumbnail(absolutePath);

    /// <summary>
    /// Stringifies this image item instance.
    /// </summary>
    /// <returns>The string representation of this image item.</returns>
    public override string ToString() => new StringBuilder(nameof(ImageItem)).Append('(')
        .Append(nameof(AbsolutePath)).Append('=').Append(AbsolutePath)
        .Append(')')
        .ToString();

    /// <summary>
    /// Compares this instance with another object for equality. This
    /// instance and the input object are considered to be equal if they are
    /// both instances of <see cref="ImageItem"/> and the
    /// <see cref="AbsolutePath"/> property of each match.
    /// </summary>
    /// <param name="obj">The object to compare this instance to.</param>
    /// <returns>True if the objects are equal based on the aforemetioned criteria.</returns>
    public override bool Equals(object? obj)
    {
        if (obj == null || obj is not ImageItem item)
        {
            return false;
        }

        return AbsolutePath == item.AbsolutePath;
    }

    /// <summary>
    /// Gets the hashcode.
    /// </summary>
    /// <returns>The hashcode.</returns>
    public override int GetHashCode() => base.GetHashCode();
}