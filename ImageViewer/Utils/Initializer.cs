namespace ImageViewer.Utils;

using ImageViewer.Models;
using ImageViewer.State;

public interface IInitializer
{
    bool InitializeAppFromPath(PathLike path);
}

public sealed class Initializer(IAppState appState, IScan scan) : IInitializer
{
    private readonly ConsoleLogger<Initializer> logger = new();
    private readonly IAppState appState = appState;
    private readonly IScan scan = scan;

    public bool InitializeAppFromPath(PathLike path)
    {
        if (path.IsDirectory())
        {
            logger.Log("Argument is a directory. Setting start path to launch argument.");
            appState.LoadStartingPath(path);
            return true;
        }
        else if (path.IsFile())
        {
            logger.Log("Argument is a file. Attempting to set start path from file path.");
            if (!scan.IsPotentiallyImageFile(path))
            {
                logger.Log("Argument was a file but didn't have a supported image extension.");
                return false;
            }

            PathLike parentDirectory = path.Parent();
            logger.Log($"Setting initial start path to: [{parentDirectory.PathString}].");
            appState.LoadStartingPath(parentDirectory);

            ImageResource? resource = appState.SelectedFolderResources
                .Find(image => image.Path.Equals(path));
            if (resource == null)
            {
                logger.Log($"Could not find file [{path.PathString}] within parent directory [{parentDirectory.PathString}].");
                return true;
            }
            logger.Log($"Setting initial selected image to [{resource.Path.PathString}].");

            appState.SelectedImage = resource;

            return true;
        }

        return false;
    }
}