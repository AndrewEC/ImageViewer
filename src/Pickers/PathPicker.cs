namespace ImageViewer.Pickers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ImageViewer.Models;

/// <summary>
/// Static utility class to help prompt the user to pick a folder.
/// </summary>
internal static class PathPicker
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
    /// Asyncronously presents a dialog so the user can select 0 or one folders.
    /// </summary>
    /// <returns>The absolute path to the folder, if one has been selected, otherwise
    /// an empty string.</returns>
    public static async Task<string> PickFolder()
    {
        Window? window = GetMainWindow();
        if (window == null)
        {
            return string.Empty;
        }

        IStorageFolder? startLocation = await window.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Downloads);

        IReadOnlyList<IStorageFolder> folder = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            SuggestedStartLocation = startLocation,
        });

        if (folder.Count == 0)
        {
            return string.Empty;
        }

        return Path.GetFullPath(Uri.UnescapeDataString(folder[0].Path.AbsolutePath.ToString()));
    }

    /// <summary>
    /// Asyncronously presents a dialog so the user can select 0 or one images.
    /// </summary>
    /// <returns>The absolute path to the image, if one has been selected, otherwise
    /// an empty string.</returns>
    public static async Task<string> PickImage()
    {
        Window? window = GetMainWindow();
        if (window == null)
        {
            return string.Empty;
        }

        IStorageFolder? startLocation = await window.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Downloads);

        IReadOnlyList<IStorageFile> files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            FileTypeFilter = [LosslessFilePickerTypes()],
            SuggestedStartLocation = startLocation,
        });

        if (files.Count == 0)
        {
            return string.Empty;
        }

        return files[0].Path.AbsolutePath.ToString();
    }

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
        string[] files = GetSupportedImagesInFolder(directory);

        if (files.Length == 0)
        {
            return null;
        }

        string displayName = Path.GetFileName(directory);
        return new(Path.GetFullPath(directory), displayName, files[0]);
    }

    private static FilePickerFileType LosslessFilePickerTypes() => new("Images")
    {
        Patterns = SupportedImageExtensions.Select(ext => $"*{ext}").ToArray(),
        AppleUniformTypeIdentifiers = ["public.image"],
        MimeTypes = ["image/*"],
    };

    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is Window window)
            {
                return window;
            }
        }

        return null;
    }
}