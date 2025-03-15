namespace ImageViewer.Util;

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using ImageViewer.Models;

/// <summary>
/// A static utility class to help lookup folders and images in
/// specified input paths.
/// </summary>
public static class PathLookup
{
    /// <summary>
    /// The array of supported image file extensions.
    /// </summary>
    public static readonly ImmutableArray<string> SupportedImageExtensions = [
        ".png",
        ".jpg",
        ".webp",
        ".jpeg"
    ];

    /// <summary>
    /// Returns an array representing all the absolute paths to the image
    /// files with a supported format that are directly under the input directory.
    /// This will not recursively scan the folder. It will only scan files
    /// directly within. If the directory does not exist this will return an
    /// empty array.
    /// </summary>
    /// <param name="directory">The directory to scan for images within.</param>
    /// <returns>An array representing the absolute paths to the images found.</returns>
    public static string[] GetSupportedImagesInFolder(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.GetFiles(directory)
            .Where(file => SupportedImageExtensions.Contains(Path.GetExtension(file)))
            .Select(Path.GetFullPath)
            .ToArray();
    }

    /// <summary>
    /// Gets an array of all the folders that exist within the input path. If the input
    /// path does not exist or is not a directory then this will return an empty array.
    /// </summary>
    /// <param name="path">The absolute path to the folder to be scanned.</param>
    /// <returns>An array of all the folders that could be found within the specified path.</returns>
    public static FolderItem[] GetValidSubFolders(string path)
    {
        if (!Directory.Exists(path))
        {
            return [];
        }

        return Directory.GetDirectories(path)
            .Select(MapToFolderItem)
            .Where(folder => folder != null)
            .OfType<FolderItem>()
            .ToArray();
    }

    private static FolderItem? MapToFolderItem(string directory)
    {
        string[] files = PathLookup.GetSupportedImagesInFolder(directory);

        if (files.Length == 0)
        {
            return null;
        }

        string displayName = Path.GetFileName(directory);
        return new(Path.GetFullPath(directory), displayName, files[0]);
    }
}