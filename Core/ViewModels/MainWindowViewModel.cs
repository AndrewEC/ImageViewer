namespace ImageViewer.Core.ViewModels;

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using ImageViewer.Core.Models;
using ImageViewer.Core.Utils;
using ImageViewer.Core.Views;
using ReactiveUI;

public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly ImmutableArray<int> SlideshowRowHeights = [0, 100];

    private readonly ConsoleLogger<MainWindowViewModel> logger = new();

    private readonly MainWindow mainWindow;
    private readonly string[] launchArguments;
    private readonly Grid mainGrid;
    private readonly TabStripItem firstItem;
    private WindowState? previousWindowState;

    public MainWindowViewModel(MainWindow mainWindow, string[] launchArguments)
    {
        logger.Log($"Application launched with [{launchArguments.Length}] arguments.");
        this.launchArguments = launchArguments;

        Refresh();

        this.mainWindow = mainWindow;
        mainWindow.KeyUp += OnKeyUp;
        mainWindow.Resized += OnMainWindowResized;

        Handlers.RegisterPropertyChangeHandler(AppState.Instance, new()
        {
            {
                nameof(AppState.Instance.IsSlideshowRunning),
                () => IsSlideshowRunning = AppState.Instance.IsSlideshowRunning
            },
            {
                nameof(AppState.Instance.SelectedTabIndex),
                () => SelectedTabIndex = AppState.Instance.SelectedTabIndex
            },
        });

        mainGrid = mainWindow.FindControl<Grid>("MainGrid")!;

        firstItem = mainWindow.FindControl<TabStripItem>("FirstTabStripItem")!;
    }

    private ImmutableArray<int> normalRowHeights = [0, 0];
    public ImmutableArray<int> NormalRowHeights
    {
        get => normalRowHeights;
        set
        {
            this.RaiseAndSetIfChanged(ref normalRowHeights, value);
            GridUtil.ResizeRows(mainGrid, value, GridUnitType.Pixel);
        }
    }

    private bool explorerView = true;
    public bool ExplorerView
    {
        get => explorerView;
        set => this.RaiseAndSetIfChanged(ref explorerView, value);
    }

    private bool imageView;
    public bool ImageView
    {
        get => imageView;
        set => this.RaiseAndSetIfChanged(ref imageView, value);
    }

    private bool settingsView;
    public bool SettingsView
    {
        get => settingsView;
        set => this.RaiseAndSetIfChanged(ref settingsView, value);
    }

    private bool isSlideshowRunning;
    public bool IsSlideshowRunning
    {
        get => isSlideshowRunning;
        set
        {
            this.RaiseAndSetIfChanged(ref isSlideshowRunning, value);
            if (value)
            {
                logger.Log("Slideshow started. Making window full screen.");

                GridUtil.ResizeRows(mainGrid, SlideshowRowHeights);

                previousWindowState = mainWindow.WindowState;
                mainWindow.WindowState = WindowState.FullScreen;
            }
            else
            {
                GridUtil.ResizeRows(mainGrid, NormalRowHeights);

                if (previousWindowState != null)
                {
                    logger.Log($"Slideshow stopped. Restoring window state: [{previousWindowState}]");
                    mainWindow.WindowState = (WindowState)previousWindowState;
                    previousWindowState = null;
                }
                else
                {
                    logger.Log("Slideshow stopped. No previous window state was recorded. Resetting to Normal.");
                    mainWindow.WindowState = WindowState.Normal;
                }
            }
        }
    }

    private int selectedTabIndex = AppState.Instance.SelectedTabIndex;
    public int SelectedTabIndex
    {
        get => selectedTabIndex;
        set
        {
            this.RaiseAndSetIfChanged(ref selectedTabIndex, value);

            ExplorerView = false;
            ImageView = false;
            SettingsView = false;

            if (value == 0)
            {
                ExplorerView = true;
            }
            else if (value == 1)
            {
                ImageView = true;
            }
            else if (value == 2)
            {
                SettingsView = true;
            }
        }
    }

    private void OnMainWindowResized(object? sender, WindowResizedEventArgs e)
    {
        if (sender == mainWindow && !AppState.Instance.IsSlideshowRunning)
        {
            NormalRowHeights = [(int)firstItem.DesiredSize.Height,
                (int)(e.ClientSize.Height - firstItem.DesiredSize.Height)];
        }
    }

    private void Refresh()
    {
        logger.Log("Refreshing app...");
        if (AppState.Instance.SelectedFolder == null)
        {
            if (!RefreshUsingLaunchArgument())
            {
                RefreshUsingDefaultLocation();
            }
        }
        else
        {
            RefreshUsingUserSelectedPath();
        }
    }

    private bool RefreshUsingLaunchArgument()
    {
        if (launchArguments.Length == 0)
        {
            return false;
        }

        logger.Log("Attempting to refresh using launch argument...");

        foreach (string argument in launchArguments)
        {
            logger.Log($"Checking argument: [{argument}].");

            PathLike argumentPath = new(argument);
            if (argumentPath.IsDirectory())
            {
                logger.Log("Argument is a directory. Setting start path to launch argument.");
                AppState.Instance.LoadStartingPath(argumentPath);
                return true;
            }
            else if (argumentPath.IsFile())
            {
                logger.Log("Argument is a file. Attempting to set start path from file path.");
                if (!Scan.IsPotentiallyImageFile(argumentPath))
                {
                    logger.Log("Argument was a file but didn't have a supported image extension.");
                    return false;
                }

                PathLike parentDirectory = argumentPath.GetParentDirectory();
                logger.Log($"Setting initial start path to: [{parentDirectory.PathString}].");
                AppState.Instance.LoadStartingPath(parentDirectory);

                ImageResource? resource = AppState.Instance.SelectedFolderResources
                    .Find(image => image.Path.Equals(argumentPath));
                if (resource == null)
                {
                    logger.Log($"Could not find file [{argumentPath.PathString}] within parent directory [{parentDirectory.PathString}].");
                    return true;
                }
                logger.Log($"Setting initial selected image to [{resource.Path.PathString}].");

                // Delay the simulated "selection" of an image after the loading parent folder.
                // Resolves an issue where the app may stay on the Explorer tab instead of
                // navigating to the Image tab.
                Task.Run(async () =>
                {
                    await Task.Delay(500);
                    AppState.Instance.SelectedImage = resource;
                });

                return true;
            }
        }

        return false;
    }

    private void RefreshUsingDefaultLocation()
    {
        string desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        PathLike downloadFolder = new PathLike(desktopFolder).GetParentDirectory().Join("Downloads");

        if (!downloadFolder.IsDirectory())
        {
            PathLike initialPath = new(AppContext.BaseDirectory);
            logger.Log($"Could not find user downloads folder. Setting initial path to: [{initialPath.PathString}]");
            AppState.Instance.LoadStartingPath(initialPath);
        }
        else
        {
            logger.Log($"Found user downloads folder. Setting initial path to [{downloadFolder.PathString}]");
            AppState.Instance.LoadStartingPath(downloadFolder);
        }
    }

    private void RefreshUsingUserSelectedPath()
    {
        PathLike initialPath = AppState.Instance.SelectedFolder!.Path;
        logger.Log($"Using previously selected folder as starting path: [{initialPath}]");
        AppState.Instance.LoadStartingPath(initialPath);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (sender == mainWindow && e.Key == Key.F5)
        {
            Refresh();
        }
    }
}
