namespace ImageViewer.Util;

using System.Collections.Generic;
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
    public static string[] GetSupportedImagesInFolder(string? directory)
    {
        if (directory == null || !Directory.Exists(directory))
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
    /// <param name="path">The absolute path to the root folder to be scanned.</param>
    /// <param name="root">The currently selected root folder which the path is either a
    /// direct or indirect child of.</param>
    /// <param name="includeRoot">Indicates whether the root folder itself should be returned
    /// as an element of the array of if it should be skipped.</param>
    /// <returns>An array of all the folders that could be found within the specified path.</returns>
    public static FolderItem[] RecursivelyFindSubFolders(
        string? path,
        string? root,
        bool includeRoot = false)
    {
        if (path == null || !Directory.Exists(path))
        {
            return [];
        }

        if (root != null && !root.EndsWith('\\'))
        {
            root += '\\';
        }

        // Skip of one means we will skip the root folder.
        int skip = includeRoot ? 0 : 1;

        return YieldSubFolders(path)
            .Skip(skip)
            .Select(subPath => MapToFolderItem(subPath, root ?? string.Empty))
            .Where(folder => folder != null)
            .OfType<FolderItem>()
            .ToArray();
    }

    /// <summary>
    /// Checks if the path points to a file that has one of the supported image
    /// extension specified by <see cref="SupportedImageExtensions"/>.
    /// </summary>
    /// <param name="path">The absolute path to the image file to check.</param>
    /// <returns>True if the path points to a file that has a supported extension.
    /// Otherwise, false.</returns>
    public static bool IsSupportedImage(string? path)
        => SupportedImageExtensions.Contains(Path.GetExtension(path) ?? string.Empty);

    private static IEnumerable<string> YieldSubFolders(string path)
    {
        yield return path;
        foreach (string directory in Directory.GetDirectories(path))
        {
            foreach (string subDir in YieldSubFolders(directory))
            {
                yield return subDir;
            }
        }
    }

    private static FolderItem? MapToFolderItem(string directory, string rootDirectory)
    {
        string? coverImage = GetSupportedImagesInFolder(directory).FirstOrDefault();

        string displayName = directory.Substring(rootDirectory.Length);
        if (directory == rootDirectory)
        {
            displayName = new DirectoryInfo(directory).Name;
        }

        return new(Path.GetFullPath(directory), displayName, coverImage);
    }
}