namespace ImageViewer.Core.ViewModels;

using System;
using System.Diagnostics;
using System.Reactive;
using ImageViewer.Core.Config;
using ImageViewer.Core.Models;
using ImageViewer.Core.Utils;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ConsoleLogger<SettingsViewModel> logger = new();

    private Config initialConfig;

    public SettingsViewModel()
    {
        SaveConfigCommand = ReactiveCommand.Create(SaveConfig);
        ResetConfigCommand = ReactiveCommand.Create(ResetConfig);
        OpenSettingsFolderCommand = ReactiveCommand.Create(OpenSettingsFolder);

        initialConfig = ConfigState.Instance.LoadConfig();
        slideshowIntervalSeconds = initialConfig.SlideshowIntervalMillis / 1000.0;
        scanDepthSelectedIndex = initialConfig.ScanDepth - 1;
        sortMethodIndex = (int)initialConfig.SortMethod;
    }

    public ReactiveCommand<Unit, Unit> SaveConfigCommand { get; }

    public ReactiveCommand<Unit, Unit> ResetConfigCommand { get; }

    public ReactiveCommand<Unit, Unit> OpenSettingsFolderCommand { get; }

    private bool hasUnsavedChanges;

    public bool HasUnsavedChanges
    {
        get => hasUnsavedChanges;
        set => this.RaiseAndSetIfChanged(ref hasUnsavedChanges, value);
    }

    private double slideshowIntervalSeconds;

    public double SlideshowIntervalSeconds
    {
        get => slideshowIntervalSeconds;
        set
        {
            this.RaiseAndSetIfChanged(ref slideshowIntervalSeconds, Math.Round(value));
            HasUnsavedChanges = true;
        }
    }

    private int scanDepthSelectedIndex;

    public int ScanDepthSelectedIndex
    {
        get => scanDepthSelectedIndex;
        set
        {
            this.RaiseAndSetIfChanged(ref scanDepthSelectedIndex, value);
            HasUnsavedChanges = true;
        }
    }

    private int sortMethodIndex;

    public int SortMethodIndex
    {
        get => sortMethodIndex;
        set
        {
            this.RaiseAndSetIfChanged(ref sortMethodIndex, value);
            HasUnsavedChanges = true;
        }
    }

    private void SaveConfig()
    {
        logger.Log("Saving configuration changes...");
        HasUnsavedChanges = false;

        Config updatedConfig = new()
        {
            SlideshowIntervalMillis = (int)(SlideshowIntervalSeconds * 1000),
            ScanDepth = ScanDepthSelectedIndex + 1,
            SortMethod = (SortMethod)SortMethodIndex,
        };
        ConfigState.Instance.SaveConfig(updatedConfig);
        initialConfig = updatedConfig;
    }

    private void ResetConfig()
    {
        logger.Log("Resetings configuration UI...");
        SlideshowIntervalSeconds = initialConfig.SlideshowIntervalMillis / 1000.0;
        ScanDepthSelectedIndex = initialConfig.ScanDepth - 1;
        SortMethodIndex = (int)initialConfig.SortMethod;

        HasUnsavedChanges = false;
    }

    private async void OpenSettingsFolder()
    {
        PathLike? path = ConfigState.Instance.CreateAndGetConfigFilePath();
        if (path == null)
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Config Not Found",
                "The configuration folder could not be found.",
                ButtonEnum.Ok)
                .ShowAsync();
            
            return;
        }

        Process.Start("explorer.exe", "/select," + path.PathString);
    }
}