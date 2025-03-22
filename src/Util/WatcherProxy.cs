namespace ImageViewer.Util;

using System.IO;
using System.Linq;
using ImageViewer.Log;
using ImageViewer.Models;

/// <summary>
/// A proxy class to help manage the <see cref="FileSystemWatcher"/> to watch for
/// changes in the selected root directory. The watcher will be configured to watch
/// for renamed, deleted, and created events within the root folder.
/// </summary>
/// <param name="appState">The global app state container.</param>
internal sealed class WatcherProxy(AppStateProperties appState)
{
    private readonly ConsoleLogger<WatcherProxy> logger = new();
    private readonly AppStateProperties appState = appState;
    private FileSystemWatcher? watcher;

    /// <summary>
    /// Stars the <see cref="FileSystemWatcher"/>. If a file system watcher
    /// is already running then it will be stopped and disposed before a new
    /// watcher is created.
    /// </summary>
    /// <param name="rootFolder">The folder for the watcher to report
    /// changes on.</param>
    public void StartFileSystemWatcher(PathLike? rootFolder)
    {
        logger.Log("Starting file system watcher.");
        if (rootFolder == null)
        {
            if (watcher == null)
            {
                return;
            }

            logger.Log("Stopping file system watcher.");
            watcher.Deleted -= OnFileSystemChanged;
            watcher.Dispose();
            watcher = null;
            return;
        }

        logger.Log($"Starting file system watcher on path [{rootFolder}].");
        watcher = new FileSystemWatcher(rootFolder.PathString);
        watcher.Deleted += OnFileSystemChanged;
        watcher.Renamed += OnFileSystemChanged;
        watcher.Created += OnFileSystemChanged;
        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;
    }

    private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        if (sender != watcher)
        {
            return;
        }

        logger.Log($"Detected file change on root path [{appState.SelectedRootFolder}].");

        PathLike fullPath = new(e.FullPath);

        switch (e.ChangeType)
        {
            case WatcherChangeTypes.Renamed:
                HandlePathRenamed(fullPath);
                break;
            case WatcherChangeTypes.Deleted:
                HandlePathDeleted(fullPath);
                break;
            case WatcherChangeTypes.Created:
                HandlePathCreated(fullPath);
                break;
        }
    }

    private void HandlePathCreated(PathLike fullPath)
    {
        logger.Log($"Detected path created at [{fullPath}].");
        if (fullPath.IsFile())
        {
            if (WasImageInCurrentlySelectedFolder(fullPath))
            {
                logger.Log("Path points to image in currently selected folder. Adding image.");
                appState.AddImage(fullPath);
            }
            else
            {
                logger.Log("Path points to an image but is not in currently selected folder. No changes to apply.");
            }
        }
        else if (fullPath.IsDirectory())
        {
            logger.Log("Path points to a folder within root folder. Adding folder.");
            appState.AddFolder(fullPath);
        }
    }

    private void HandlePathRenamed(PathLike fullPath)
    {
        logger.Log($"Detected path renamed to [{fullPath}].");

        if (fullPath.IsDirectory())
        {
            FolderResource? oldFolder = GetDirectoryThatWasRenamed();
            if (oldFolder != null)
            {
                appState.RemoveFolder(oldFolder.Path);
            }

            appState.AddFolder(fullPath);
        }
        else if (fullPath.IsFile())
        {
            ImageResource? oldImage = GetImageThatWasRenamed();
            if (oldImage != null)
            {
                appState.RemoveImage(oldImage.Path);
            }

            appState.AddImage(fullPath);
        }
    }

    private void HandlePathDeleted(PathLike fullPath)
    {
        logger.Log($"Detected path was deleted from [{fullPath}].");
        if (WasFolder(fullPath))
        {
            logger.Log("Deleted path appears to have been a folder. Removing folder from available array.");
            appState.RemoveFolder(fullPath);
        }
        else if (WasImageInCurrentlySelectedFolder(fullPath))
        {
            logger.Log("Deleted path appears to have been an image in currently selected folder. Removing image from available array.");
            appState.RemoveImage(fullPath);
        }
    }

    private bool WasFolder(PathLike path) => appState.Folders.WhereByPath(path).Any();

    private ImageResource? GetImageThatWasRenamed() => appState.Images
        .Where(image => !image.Path.IsFile())
        .FirstOrDefault();

    private FolderResource? GetDirectoryThatWasRenamed() => appState.Folders
        .Where(folder => !folder.Path.IsDirectory())
        .FirstOrDefault();

    private bool WasImageInCurrentlySelectedFolder(PathLike path)
    {
        PathLike? parentFolder = path.GetParentDirectory();
        return parentFolder != null
            && appState.SelectedFolder?.Path == parentFolder;
    }
}