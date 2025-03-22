namespace ImageViewer.Models;

using System;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using ImageViewer.Util;

/// <summary>
/// Represents a single folder within the user's selected root folder.
/// </summary>
/// <param name="absolutePath">The absolute path to the folder on disk.</param>
/// <param name="displayName">The name of the folder.</param>
/// <param name="previewImagePath">The absolute path to the first image within the folder.</param>
public sealed class FolderResource(PathLike absolutePath, string displayName, PathLike? previewImagePath) : IPathResource
{
    /// <summary>
    /// Gets the absolute path to the folder on disk.
    /// </summary>
    public PathLike Path => absolutePath;

    /// <summary>
    /// Gets the name of the folder to be displayed.
    /// </summary>
    public string DisplayName => displayName;

    /// <summary>
    /// Gets the fully qualified path to the first image of the folder.
    /// </summary>
    public PathLike? PreviewImagePath => previewImagePath;

    /// <summary>
    /// Gets a thumbnail of the first image in the folder. Sized so the width
    /// is 200 pixels in size.
    /// </summary>
    public Task<Bitmap> PreviewImage
        => ImageCache.Instance.LoadThumbnailAsync(PreviewImagePath);

    /// <summary>
    /// Stringifies this folder item instance.
    /// </summary>
    /// <returns>A string representation of this folder item.</returns>
    public override string ToString() => new StringBuilder(nameof(FolderResource)).Append('(')
        .Append(nameof(Path)).Append('=').Append(Path.PathString).Append(", ")
        .Append(nameof(DisplayName)).Append('=').Append(DisplayName).Append(", ")
        .Append(nameof(PreviewImagePath)).Append('=').Append(PreviewImagePath)
        .Append(')')
        .ToString();

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj != null && obj is FolderResource other
            && Path == other.Path && DisplayName == other.DisplayName;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Path, DisplayName, PreviewImagePath);
}