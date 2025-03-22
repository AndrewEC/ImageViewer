namespace ImageViewer.Models;

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ImageViewer.Log;
using ImageViewer.Util;

/// <summary>
/// A global state container containing all shared properties of the app.
/// </summary>
[SuppressMessage("Ordering Rules", "SA1201", Justification = "Reviewed.")]
public sealed class AppStateProperties : INotifyPropertyChanged
{
    private readonly ConsoleLogger<AppStateProperties> logger = new();

    private PathLike? selectedRootFolder = null;

    /// <summary>
    /// Gets or sets the currently selected folder. Upon setting this to a
    /// non-null value this will trigger an update in the <see cref="Folders"/>
    /// by scanning the directly nested folders of the updated root folder
    /// to find folders that contain at least one image.
    /// </summary>
    public PathLike? SelectedRootFolder
    {
        get => selectedRootFolder;
        set
        {
            if (!UpdateIfChanged(nameof(SelectedRootFolder), ref selectedRootFolder, value))
            {
                return;
            }

            if (value != null)
            {
                Folders = [
                    .. PathLikeExtensions.EnumerateChildDirectoryResources(SelectedRootFolder),
                    PathLikeExtensions.ToFolderResource(SelectedRootFolder!)
                ];
            }
        }
    }

    private FolderResource[] folders = [];

    /// <summary>
    /// Gets the list of folders that are nested under the
    /// <see cref="SelectedRootFolder"/>.
    /// </summary>
    public FolderResource[] Folders
    {
        get => folders;
        private set
        {
            FolderResource[] sorted = [.. value.OrderBy(folder => folder.Path)];
            if (!UpdateIfChanged(nameof(Folders), ref folders, sorted))
            {
                return;
            }

            SelectedFolder = Folders.FirstByPath(SelectedFolder?.Path);
        }
    }

    private FolderResource? selectedFolder = null;

    /// <summary>
    /// Gets or sets the currently selected folder. Upon being updated
    /// this will trigger an update in the <see cref="Images"/> property.
    /// This update will set the Images to an empty array, if the new value is null,
    /// or will set the array to contain a set of image files found within the
    /// folder path represented by the new value.
    /// </summary>
    public FolderResource? SelectedFolder
    {
        get => selectedFolder;
        set
        {
            if (!UpdateIfChanged(nameof(SelectedFolder), ref selectedFolder, value))
            {
                return;
            }

            SelectedFolderIndex = Array.IndexOf(Folders, value);
            Images = [.. PathLikeExtensions.EnumerateChildImageResources(value?.Path)];
        }
    }

    private int selectedFolderIndex = -1;

    /// <summary>
    /// Gets the index the <see cref="SelectedFolder"/>. has within the
    /// <see cref="Folders"/> array.
    /// </summary>
    public int SelectedFolderIndex
    {
        get => selectedFolderIndex;
        private set => UpdateIfChanged(nameof(SelectedFolderIndex), ref selectedFolderIndex, value);
    }

    private ImageResource[] images = [];

    /// <summary>
    /// Gets the array of images the user can pick from.
    /// </summary>
    public ImageResource[] Images
    {
        get => images;
        private set
        {
            ImageResource[] sorted = [.. value.OrderBy(image => image.Path)];
            if (UpdateIfChanged(nameof(Images), ref images, sorted))
            {
                SelectedImage = Images.FirstByPath(SelectedImage?.Path);
            }
        }
    }

    private ImageResource? selectedImage = null;

    /// <summary>
    /// Gets or sets the currently selected image.
    /// is null or not.
    /// </summary>
    public ImageResource? SelectedImage
    {
        get => selectedImage;
        set
        {
            if (!UpdateIfChanged(nameof(SelectedImage), ref selectedImage, value))
            {
                return;
            }

            SelectedImageIndex = Array.IndexOf(Images, value);
        }
    }

    private int selectedImageIndex = -1;

    /// <summary>
    /// Gets the index of the <see cref="SelectedImage"/>. has within the
    /// <see cref="Images"/> array.
    /// </summary>
    public int SelectedImageIndex
    {
        get => selectedImageIndex;
        private set => UpdateIfChanged(nameof(SelectedImageIndex), ref selectedImageIndex, value);
    }

    private AvailableTabs selectedTab = AvailableTabs.FolderList;

    /// <summary>
    /// Gets or sets the numeric enum representing the currently selected tab
    /// of the tab control.
    /// </summary>
    public AvailableTabs SelectedTab
    {
        get => selectedTab;
        set => UpdateIfChanged(nameof(SelectedTab), ref selectedTab, value);
    }

    private bool isSlideshowRunning = false;

    /// <summary>
    /// Gets or sets a value indicating whether the slideshow is currently
    /// running.
    /// </summary>
    public bool IsSlideshowRunning
    {
        get => isSlideshowRunning;
        set => UpdateIfChanged(nameof(IsSlideshowRunning), ref isSlideshowRunning, value);
    }

    /// <summary>
    /// An event to notify when a property within this state object changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Adds a folder with the specified path to the existing
    /// <see cref="Folders"/> array. This will also recursively lookup
    /// and add any nested folders.
    /// </summary>
    /// <param name="path">The absolute path to the folder being added.</param>
    public void AddFolder(PathLike path)
    {
        if (SelectedRootFolder == default)
        {
            return;
        }

        FolderResource[] newFolders = [.. PathLikeExtensions.EnumerateChildDirectoryResources(path)];
        Folders = [.. newFolders, .. Folders];
    }

    /// <summary>
    /// Adds an image to the current <see cref="Images"/> array. This will add the image
    /// if the image is a direct child of the <see cref="SelectedFolder"/>.
    /// </summary>
    /// <param name="path">The absolute path to the image to add.</param>
    public void AddImage(PathLike path)
    {
        if (SelectedFolder == null || !SelectedFolder.Path.IsParentOf(path, true))
        {
            return;
        }

        Images = [.. Images, new ImageResource(path)];
    }

    /// <summary>
    /// Removes a folder item with an absolute path matching the input path
    /// from the current <see cref="Folders"/> array. This will also remove any
    /// nested folders if any are present. This will have no affect if the path
    /// does not match any folder in the <see cref="Folders"/> array.
    /// </summary>
    /// <param name="path">The absolute path to the folder to remove.</param>
    public void RemoveFolder(PathLike path)
    {
        FolderResource? toRemove = Folders.FirstByPath(path);
        if (toRemove == null)
        {
            return;
        }

        FolderResource[] childFolders = [.. Folders.Where(folder => toRemove.Path.IsParentOf(folder.Path))];

        FolderResource[] removableFolderPaths = [.. childFolders, toRemove];

        Folders = [.. Folders.Where(folder => !removableFolderPaths.Contains(folder))];
    }

    /// <summary>
    /// Removes an image with a matching absolute path from the current
    /// <see cref="Images"/> array.
    /// </summary>
    /// <param name="path">The absolute path to the image to be removed.</param>
    public void RemoveImage(PathLike path)
    {
        ImageResource? toRemove = Images.FirstByPath(path);
        if (toRemove != null)
        {
            return;
        }

        ImageResource[] updated = [.. Images.Where(image => !image.Equals(toRemove))];
        if (updated.Length == Images.Length)
        {
            return;
        }

        Images = updated;
    }

    private bool UpdateIfChanged<T>(string propName, ref T currentValue, T nextValue)
    {
        logger.Log($"Attempt to change property [{propName}]");
        if (ChangeUtil.AreRoughlyEqual(currentValue, nextValue))
        {
            return false;
        }

        logger.Log($"Property [{propName}] changed from "
            + $"[{ChangeUtil.Stringify(currentValue)}] "
            + $"to [{ChangeUtil.Stringify(nextValue)}].");

        currentValue = nextValue;
        PropertyChanged?.Invoke(this, new(propName));

        return true;
    }
}
