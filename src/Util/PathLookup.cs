namespace ImageViewer.Util;

using System;
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
    /// Determines if two file paths match. This will normalize the path to use only
    /// backslash separators and remove any trailing separators before making a
    /// case insensitive comparison of the resulting string value.
    /// </summary>
    /// <param name="first">The first path to compare.</param>
    /// <param name="second">The second path to compare.</param>
    /// <returns>True if both string match based on the above criteria.</returns>
    public static bool DoWindowsPathsMatch(string? first, string? second)
    {
        if ((first == null && second != null) || (first != null && second == null))
        {
            return false;
        }

        return string.Equals(
            NormalizePath(first!),
            NormalizePath(second!),
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets an array of all the folders that exist within the input path. If the input
    /// path does not exist or is not a directory then this will return an empty array.
    /// </summary>
    /// <param name="path">The absolute path to the folder to be scanned.</param>
    /// <returns>An array of all the folders that could be found within the specified path.</returns>
    public static FolderItem[] GetValidSubFolders(string? path)
    {
        if (path == null || !Directory.Exists(path))
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
        string[] files = GetSupportedImagesInFolder(directory);

        if (files.Length == 0)
        {
            return null;
        }

        string displayName = Path.GetFileName(directory);
        return new(Path.GetFullPath(directory), displayName, files[0]);
    }

    private static string NormalizePath(string path)
        => Path.GetFullPath(path).Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}