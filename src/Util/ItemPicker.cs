namespace ImageViewer.Util;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

/// <summary>
/// Static utility class to help prompt the user to pick a folder.
/// </summary>
internal static class ItemPicker
{
    /// <summary>
    /// Asyncronously presents a dialog so the user can select 0 or one folders.
    /// </summary>
    /// <returns>The absolute path to the folder, if one has been selected, otherwise
    /// an empty string.</returns>
    public static async Task<string> PromptForFolder()
    {
        Window? window = WindowLookup.GetMainWindow();
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
    public static async Task<string> PromptForImage()
    {
        Window? window = WindowLookup.GetMainWindow();
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
        Patterns = PathLikeExtensions.SupportedImageExtensions.Select(ext => $"*{ext}").ToArray(),
        AppleUniformTypeIdentifiers = ["public.image"],
        MimeTypes = ["image/*"],
    };
}