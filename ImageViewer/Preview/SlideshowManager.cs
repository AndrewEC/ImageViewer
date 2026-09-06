namespace ImageViewer.Preview;

using System;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using ImageViewer.Models;
using ImageViewer.State;
using ImageViewer.Utils;
using ImageViewer.Views;

#pragma warning disable CA1001
public sealed class SlideshowManager(IAppState state, IConfigState configState) : INotifyPropertyChanged
#pragma warning restore CA1001
{
    private readonly ConsoleLogger<SlideshowManager> logger = new();

    private MainWindow? mainWindow;
    private Timer? slideshowTimer;

    private int[] imageOrder = [];
    private int currentIndex = -1;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int ImageIndex { get; set; }

    public void StartSlideshow()
    {
        logger.Log("Starting slideshow...");
        if (slideshowTimer != null)
        {
            return;
        }

        StopSlideshow();

        Config config = configState.LoadConfig();
        imageOrder = GenerateImageIndexes(config.RandomizeSlideshow);
        currentIndex = imageOrder.IndexOf(state.SelectedImageIndex);

        int interval = config.SlideshowIntervalMillis;
        logger.Log($"Starting slideshow timer with interval of: [{interval}], and randomize: [{config.RandomizeSlideshow}].");
        slideshowTimer = new Timer(interval);
        slideshowTimer.Elapsed += (sender, e) => NextImage();
        slideshowTimer.AutoReset = true;
        slideshowTimer.Enabled = true;

        mainWindow = GetMainWindow();
        mainWindow?.Cursor = new Cursor(StandardCursorType.None);
    }

    private int[] GenerateImageIndexes(bool randomize)
    {
        int[] allIndexes = Enumerable.Range(0, state.SelectedFolderResources.Count).ToArray();
        if (!randomize)
        {
            return allIndexes;
        }

        var random = new Random();
        return allIndexes.Select(index => (index, random.Next()))
            .OrderBy(tuple => tuple.Item2)
            .Select(tuple => tuple.Item1)
            .ToArray();
    }

    public void StopSlideshow()
    {
        if (slideshowTimer == null)
        {
            return;
        }

        logger.Log("Stopping slideshow timer.");
        slideshowTimer.Stop();
        slideshowTimer.Dispose();
        slideshowTimer = null;

        mainWindow?.Cursor = new Cursor(StandardCursorType.Arrow);
    }

    private void NextImage()
    {
        int nextIndex = (currentIndex + 1) % state.SelectedFolderResources.Count;
        logger.Log($"Slideshow timer ticked. Moving from index [{currentIndex}], to [{nextIndex}].");
        currentIndex = nextIndex;
        // state.SelectedImage = state.SelectedFolderResources[imageOrder[nextIndex]];
        ImageIndex = imageOrder[nextIndex];
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImageIndex)));
    }

    public int PeekNextImageIndex() => imageOrder[(currentIndex + 1) % state.SelectedFolderResources.Count];

    private static MainWindow? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            return (lifetime.MainWindow as MainWindow)!;
        }
        return null;
    }
}
