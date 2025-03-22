namespace ImageViewer.Models;

using System;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using ImageViewer.Util;

/// <summary>
/// Represents the details of a sigle image within a selected sub-folder.
/// </summary>
/// <param name="absolutePath">The absolute path to the image on disk.</param>
public sealed class ImageResource(PathLike absolutePath) : IPathResource
{
    /// <summary>
    /// Gets the absolute path to the image on disk.
    /// </summary>
    public PathLike Path => absolutePath;

    /// <summary>
    /// Gets the image bitmap. This should only be used when viewing the full image.
    /// When viewing the image as part of a list or grid the more memory efficient
    /// <see cref="Thumbnail"/> property should be preferred instead.
    /// </summary>
    public Task<Bitmap> Image => ImageCache.Instance.LoadImageAsync(Path);

    /// <summary>
    /// Gets a thumbnail of the image. Sized to maintain the original aspect ratio but
    /// be no-more than 200 pixels in width.
    /// </summary>
    public Task<Bitmap> Thumbnail => ImageCache.Instance.LoadThumbnailAsync(Path);

    /// <summary>
    /// Stringifies this image item instance.
    /// </summary>
    /// <returns>The string representation of this image item.</returns>
    public override string ToString() => new StringBuilder(nameof(ImageResource)).Append('(')
        .Append(nameof(Path)).Append('=').Append(Path)
        .Append(')')
        .ToString();

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj != null && obj is ImageResource other && other.Path == Path;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Path);
}