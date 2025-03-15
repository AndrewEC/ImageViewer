namespace ImageViewer.Util;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

/// <summary>
/// Static utility class to access functions on the main window.
/// </summary>
public static class WindowLookup
{
    /// <summary>
    /// Attempts to lookup and return the main application window.
    /// </summary>
    /// <returns>Gets the main window.</returns>
    public static Window? GetMainWindow()
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

    /// <summary>
    /// Attempts to access the main window, make it the topmost,
    /// and make it borderless fullscreen.
    /// </summary>
    public static void RequestFullScreen()
    {
        Window? window = GetMainWindow();
        if (window == null)
        {
            return;
        }

        window.Topmost = true;
        window.WindowState = WindowState.FullScreen;
    }

    /// <summary>
    /// Attempts to access the main window, make it no longer the
    /// topmost, and maximize its size.
    /// </summary>
    public static void RequestRestore()
    {
        Window? window = GetMainWindow();
        if (window == null)
        {
            return;
        }

        window.Topmost = false;
        window.WindowState = WindowState.Maximized;
    }
}