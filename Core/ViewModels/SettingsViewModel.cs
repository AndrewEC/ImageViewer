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
        SlideshowIntervalSeconds = initialConfig.SlideshowIntervalMillis / 1000.0;
        ScanDepthSelectedIndex = initialConfig.ScanDepth - 1;
        SortMethodIndex = (int)initialConfig.SortMethod;
    }

    public ReactiveCommand<Unit, Unit> SaveConfigCommand { get; }

    public ReactiveCommand<Unit, Unit> ResetConfigCommand { get; }

    public ReactiveCommand<Unit, Unit> OpenSettingsFolderCommand { get; }

    public bool HasUnsavedChanges
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public double SlideshowIntervalSeconds
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, Math.Round(value));
            HasUnsavedChanges = true;
        }
    }

    public int ScanDepthSelectedIndex
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            HasUnsavedChanges = true;
        }
    }

    public int SortMethodIndex
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
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