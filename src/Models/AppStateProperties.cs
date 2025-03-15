namespace ImageViewer.Models;

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

    private string? selectedRootFolder = null;

    /// <summary>
    /// Gets or sets the currently selected folder. Upon setting this to a
    /// non-null value this will trigger an update in the <see cref="Folders"/>
    /// by scanning the directly nested folders of the updated root folder
    /// to find folders that contain at least one image.
    /// </summary>
    public string? SelectedRootFolder
    {
        get => selectedRootFolder;
        set
        {
            if (!UpdateIfChanged(nameof(SelectedRootFolder), ref selectedRootFolder, value))
            {
                return;
            }

            RescanRootFolder();
        }
    }

    private FolderItem[] folders = [];

    /// <summary>
    /// Gets or sets the list of folders that are nested under the
    /// <see cref="SelectedRootFolder"/>. Upon being updated this
    /// will set the <see cref="SelectedFolder"/> to null.
    /// </summary>
    public FolderItem[] Folders
    {
        get => folders;
        set
        {
            if (!UpdateIfChanged(nameof(Folders), ref folders, value))
            {
                return;
            }

            SelectedFolder = ReselectItem(Folders, SelectedFolder);
        }
    }

    private FolderItem? selectedFolder = null;

    /// <summary>
    /// Gets or sets the currently selected folder. Upon being updated
    /// this will trigger an update in the <see cref="Images"/> property.
    /// This update will set the Images to an empty array, if the new value is null,
    /// or will set the array to contain a set of image files found within the
    /// folder path represented by the new value.
    /// </summary>
    public FolderItem? SelectedFolder
    {
        get => selectedFolder;
        set
        {
            if (!UpdateIfChanged(nameof(SelectedFolder), ref selectedFolder, value))
            {
                return;
            }

            RescanSelectedFolder();
        }
    }

    private ImageItem[] images = [];

    /// <summary>
    /// Gets or sets the array of images the user can pick from. Upon updating
    /// this will set the <see cref="SelectedImage"/> property to null.
    /// </summary>
    public ImageItem[] Images
    {
        get => images;
        set
        {
            if (UpdateIfChanged(nameof(Images), ref images, value))
            {
                SelectedImage = ReselectItem(Images, SelectedImage);
                if (Images.Length == 0)
                {
                    RescanRootFolder();
                }
            }
        }
    }

    private ImageItem? selectedImage = null;

    /// <summary>
    /// Gets or sets the currently selected image.
    /// is null or not.
    /// </summary>
    public ImageItem? SelectedImage
    {
        get => selectedImage;
        set => UpdateIfChanged(nameof(SelectedImage), ref selectedImage, value);
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

    public bool IsSlideshowRunning
    {
        get => isSlideshowRunning;
        set => UpdateIfChanged(nameof(IsSlideshowRunning), ref isSlideshowRunning, value);
    }

#pragma warning disable SA1201
    /// <summary>
    /// An event to notify when a property within this state object changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore SA1201

    /// <summary>
    /// Scans the currently selected root folder and updates the <see cref="Images"/>
    /// property with the images found in the folder. If no folder is currently selected
    /// the <see cref="Images"/> property will default to an empty array.
    /// </summary>
    public void RescanSelectedFolder()
    {
        if (SelectedFolder == null)
        {
            Images = [];
            return;
        }
        else
        {
            Images = PathLookup.GetSupportedImagesInFolder(SelectedFolder.AbsolutePath)
                .Select(file => new ImageItem(file))
                .ToArray();
        }
    }

    /// <summary>
    /// Scans the currently selected root folder for all nested folders and sets the
    /// <see cref="Folders"/> property. This search is not recursive. If no root
    /// folder is currently selected the <see cref="Folders"/> will be set to an empty
    /// array.
    /// </summary>
    public void RescanRootFolder()
    {
        if (SelectedRootFolder == null)
        {
            Folders = [];
        }
        else
        {
            Folders = PathLookup.GetValidSubFolders(SelectedRootFolder);
        }
    }

    private static T? ReselectItem<T>(T[] elements, T? previouslySelected)
    where T : class, IFindable
    {
        if (previouslySelected == null)
        {
            return null;
        }

        return elements.Where(element => element.AbsolutePath == previouslySelected.AbsolutePath)
            .FirstOrDefault();
    }

    private bool UpdateIfChanged<T>(string propName, ref T currentValue, T nextValue)
    {
        logger.Log($"Attempt to change property [{propName}]");
        if ((currentValue == null && nextValue == null) || (currentValue?.Equals(nextValue) ?? false))
        {
            return false;
        }

        logger.Log($"Property [{propName}] changed from [{currentValue}] to [{nextValue}].");

        currentValue = nextValue;
        PropertyChanged?.Invoke(this, new(propName));

        return true;
    }
}
