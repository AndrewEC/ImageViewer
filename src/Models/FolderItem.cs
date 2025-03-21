namespace ImageViewer.Models;

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
public sealed class FolderItem(PathLike absolutePath, string displayName, PathLike? previewImagePath) : IPathResource
{
    /// <summary>
    /// Gets the absolute path to the folder on disk.
    /// </summary>
    public PathLike Path { get; } = absolutePath;

    /// <summary>
    /// Gets the name of the folder to be displayed.
    /// </summary>
    public string DisplayName { get; } = displayName;

    /// <summary>
    /// Gets the fully qualified path to the first image of the folder.
    /// </summary>
    public PathLike? PreviewImagePath { get; } = previewImagePath;

    /// <summary>
    /// Gets a thumbnail of the first image in the folder. Sized so the width
    /// is 200 pixels in size.
    /// </summary>
    public Task<Bitmap> PreviewImage
    {
        get => ImageCache.Instance.LoadThumbnailAsync(PreviewImagePath?.PathString);
    }

    /// <summary>
    /// Stringifies this folder item instance.
    /// </summary>
    /// <returns>A string representation of this folder item.</returns>
    public override string ToString() => new StringBuilder(nameof(FolderItem)).Append('(')
        .Append(nameof(Path)).Append('=').Append(Path.PathString).Append(", ")
        .Append(nameof(DisplayName)).Append('=').Append(DisplayName).Append(", ")
        .Append(nameof(PreviewImagePath)).Append('=').Append(PreviewImagePath).Append(", ")
        .Append(')')
        .ToString();
}