namespace ImageViewer.Pickers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

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

        return Uri.UnescapeDataString(folder[0].Path.AbsolutePath.ToString());
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