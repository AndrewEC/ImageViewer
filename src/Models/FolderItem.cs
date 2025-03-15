namespace ImageViewer.Models;

using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

/// <summary>
/// Represents a single folder within the user's selected root folder.
/// </summary>
/// <param name="absolutePath">The absolute path to the folder on disk.</param>
/// <param name="displayName">The name of the folder.</param>
/// <param name="previewImagePath">The absolute path to the first image within the folder.</param>
public sealed class FolderItem(string absolutePath, string displayName, string previewImagePath) : IFindable
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
    public string PreviewImagePath { get; } = previewImagePath;

    /// <summary>
    /// Gets a thumbnail of the first image in the folder. Sized so the width
    /// is 200 pixels in size.
    /// </summary>
    public Task<Bitmap> PreviewImage { get; }
        = Task.Run(() =>
        {
            MemoryStream stream = new(File.ReadAllBytes(previewImagePath));
            return Bitmap.DecodeToWidth(stream, 200, BitmapInterpolationMode.LowQuality);
        });

    /// <summary>
    /// Stringifies this folder item instance.
    /// </summary>
    /// <returns>A string representation of this folder item.</returns>
    public override string ToString() => string.Format($"FolderItem(AbsolutePath={AbsolutePath}, DisplayName={DisplayName}, PreviewImagePath={PreviewImagePath})");
}