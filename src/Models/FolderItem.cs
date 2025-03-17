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
public sealed class FolderItem(string absolutePath, string displayName, string? previewImagePath) : IFindable
{
    /// <summary>
    /// Gets the absolute path to the folder on disk.
    /// </summary>
    public string AbsolutePath { get; } = absolutePath;

    /// <summary>
    /// Gets the name of the folder to be displayed.
    /// </summary>
    public string DisplayName { get; } = displayName;

    /// <summary>
    /// Gets the fully qualified path to the first image of the folder.
    /// </summary>
    public string? PreviewImagePath { get; } = previewImagePath;

    /// <summary>
    /// Gets a thumbnail of the first image in the folder. Sized so the width
    /// is 200 pixels in size.
    /// </summary>
    public Task<Bitmap?> PreviewImage { get; } = ThumbnailCache.Instance.LoadThumbnail(previewImagePath);

    /// <summary>
    /// Stringifies this folder item instance.
    /// </summary>
    /// <returns>A string representation of this folder item.</returns>
    public override string ToString() => new StringBuilder(nameof(FolderItem)).Append('(')
        .Append(nameof(AbsolutePath)).Append('=').Append(AbsolutePath).Append(", ")
        .Append(nameof(DisplayName)).Append('=').Append(DisplayName).Append(", ")
        .Append(nameof(PreviewImagePath)).Append('=').Append(PreviewImagePath).Append(", ")
        .Append(')')
        .ToString();

    /// <summary>
    /// Compares this instance with another object for equality. This
    /// instance and the input object are considered to be equal if they are
    /// both instances of <see cref="FolderItem"/> and the
    /// <see cref="AbsolutePath"/>, <see cref="DisplayName"/>, and <see cref="PreviewImagePath"/>
    /// properties of each match.
    /// </summary>
    /// <param name="obj">The object to compare this instance to.</param>
    /// <returns>True if the objects are equal based on the aforemetioned criteria.</returns>
    public override bool Equals(object? obj)
    {
        if (obj == null || obj is not FolderItem item)
        {
            return false;
        }

        return AbsolutePath == item.AbsolutePath
            && DisplayName == item.DisplayName
            && PreviewImagePath == item.PreviewImagePath;
    }

    /// <summary>
    /// Gets the hashcode.
    /// </summary>
    /// <returns>The hashcode.</returns>
    public override int GetHashCode() => base.GetHashCode();
}