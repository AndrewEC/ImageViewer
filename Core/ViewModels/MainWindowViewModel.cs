namespace ImageViewer.Core.ViewModels;

using System;
using System.Collections.Immutable;
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
    private WindowState? previousWindowState = WindowState.Maximized;

    public MainWindowViewModel(MainWindow mainWindow, string[] launchArguments)
    {
        logger.Log($"Application launched with [{launchArguments.Length}] arguments.");
        this.launchArguments = launchArguments;
        this.mainWindow = mainWindow;

        mainWindow.KeyUp += OnKeyUp;
        mainWindow.Resized += OnMainWindowResized;
        mainWindow.WindowState = (WindowState)previousWindowState;

        mainGrid = mainWindow.FindControl<Grid>("MainGrid")!;
        firstItem = mainWindow.FindControl<TabStripItem>("FirstTabStripItem")!;

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

        Refresh();
    }

    public ImmutableArray<int> NormalRowHeights
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            GridUtil.ResizeRows(mainGrid, value, GridUnitType.Pixel);
        }
    } = [0, 0];

    public bool ExplorerView
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = true;

    public bool ImageView
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool SettingsView
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IsSlideshowRunning
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            OnSlideshowStateChanged(value);
        }
    }

    public int SelectedTabIndex
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);

            ExplorerView = value == 0;
            ImageView = value == 1;
            SettingsView = value == 2;
        }
    } = AppState.Instance.SelectedTabIndex;

    private void OnSlideshowStateChanged(bool value)
    {
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

                PathLike parentDirectory = argumentPath.Parent();
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

                AppState.Instance.SelectedImage = resource;

                return true;
            }
        }

        return false;
    }

    private void RefreshUsingDefaultLocation()
    {
        string desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        PathLike downloadFolder = new PathLike(desktopFolder).Parent().Join("Downloads");

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
