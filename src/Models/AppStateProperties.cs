namespace ImageViewer.Models;

using System.ComponentModel;
using System.IO;
using System.Linq;
using ImageViewer.Log;
using ImageViewer.Pickers;

public sealed class AppStateProperties : INotifyPropertyChanged
{
    private readonly ConsoleLogger<AppStateProperties> logger = new();

    private string? selectedRootFolder = null;

    public string? SelectedRootFolder
    {
        get => selectedRootFolder;
        set
        {
            if (!UpdateIfChanged(nameof(SelectedRootFolder), ref selectedRootFolder, value))
            {
                return;
            }

            if (value == null)
            {
                Folders = [];
            }
            else
            {
                Folders = GetSubFolderItems(value);
            }
        }
    }

    private FolderItem[] folders = [];

    public FolderItem[] Folders
    {
        get => folders;
        set
        {
            if (UpdateIfChanged(nameof(Folders), ref folders, value))
            {
                SelectedFolder = null;
            }
        }
    }

    private FolderItem? selectedFolder = null;

    public FolderItem? SelectedFolder
    {
        get => selectedFolder;
        set
        {
            if (!UpdateIfChanged(nameof(SelectedFolder), ref selectedFolder, value))
            {
                return;
            }

            if (value == null)
            {
                Images = [];
                return;
            }
            else
            {
                Images = GetSupportedImagesInFolder(value.AbsolutePath)
                    .Select(file => new ImageItem(file))
                    .ToArray();
            }
        }
    }

    private ImageItem[] images = [];

    public ImageItem[] Images
    {
        get => images;
        set
        {
            if (UpdateIfChanged(nameof(Images), ref images, value))
            {
                SelectedImage = null;
            }
        }
    }

    private ImageItem? selectedImage = null;

    public ImageItem? SelectedImage
    {
        get => selectedImage;
        set
        {
            if (!UpdateIfChanged(nameof(SelectedImage), ref selectedImage, value))
            {
                return;
            }

            IsImageSelected = value != null;
        }
    }

    private bool isImageSelected = false;

    public bool IsImageSelected
    {
        get => isImageSelected;
        set => UpdateIfChanged(nameof(IsImageSelected), ref isImageSelected, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private static string[] GetSupportedImagesInFolder(string directory) => Directory.GetFiles(directory)
            .Where(file => PathPicker.SupportedImageExtensions.Contains(Path.GetExtension(file)))
            .ToArray();

    private static FolderItem[] GetSubFolderItems(string path)
        => Directory.GetDirectories(path)
            .Select(MapToFolderItem)
            .Where(folder => folder != null)
            .OfType<FolderItem>()
            .ToArray();

    private static FolderItem? MapToFolderItem(string directory)
    {
        string[] files = GetSupportedImagesInFolder(directory);

        if (files.Length == 0)
        {
            return null;
        }

        string displayName = Path.GetFileName(directory);
        return new(directory, displayName, files[0]);
    }

    private bool UpdateIfChanged<T>(string propName, ref T currentValue, T nextValue)
    {
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
