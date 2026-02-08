namespace ImageViewer.ViewModels;

using System;
using System.Diagnostics;
using System.Reactive;
using ImageViewer.Injection;
using ImageViewer.Models;
using ImageViewer.State;
using ImageViewer.Utils;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ConsoleLogger<SettingsViewModel> logger = new();

    private readonly IConfigState configState;
    private Config initialConfig;

    public SettingsViewModel()
    {
        configState = ServiceContainer.GetService<IConfigState>();

        SaveConfigCommand = ReactiveCommand.Create(SaveConfig);
        ResetConfigCommand = ReactiveCommand.Create(ResetConfig);
        OpenSettingsFolderCommand = ReactiveCommand.Create(OpenSettingsFolder);

        initialConfig = configState.LoadConfig();
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
            HasUnsavedChanges = HasConfigChanged();
        }
    }

    public int ScanDepthSelectedIndex
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            HasUnsavedChanges = HasConfigChanged();
        }
    }

    public int SortMethodIndex
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            HasUnsavedChanges = HasConfigChanged();
        }
    }

    private bool HasConfigChanged() => !DeriveConfig().Equals(initialConfig);

    private Config DeriveConfig() => new()
    {
        SlideshowIntervalMillis = (int)(SlideshowIntervalSeconds * 1000),
        ScanDepth = ScanDepthSelectedIndex + 1,
        SortMethod = (SortMethod)SortMethodIndex,
    };

    private async void SaveConfig()
    {
        logger.Log("Saving configuration changes...");
        HasUnsavedChanges = false;

        Config updatedConfig = DeriveConfig();

        if (!configState.SaveConfig(updatedConfig))
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Save Failed",
                "Your settings could not be saved. The config file may be in use or this program may not have permission to write to it.",
                ButtonEnum.Ok)
                .ShowAsync();
            return;
        }

        initialConfig = updatedConfig;
    }

    private void ResetConfig()
    {
        logger.Log("Resetting configuration UI...");
        SlideshowIntervalSeconds = initialConfig.SlideshowIntervalMillis / 1000.0;
        ScanDepthSelectedIndex = initialConfig.ScanDepth - 1;
        SortMethodIndex = (int)initialConfig.SortMethod;

        HasUnsavedChanges = false;
    }

    private async void OpenSettingsFolder()
    {
        PathLike path = configState.GetConfigFilePath();
        if (!path.IsFile())
        {
            await MessageBoxManager.GetMessageBoxStandard(
                "Config Not Found",
                "No configuration folder exists. One will be created when you update any settings.",
                ButtonEnum.Ok)
                .ShowAsync();
            
            return;
        }

        Process.Start("explorer.exe", "/select," + path.PathString);
    }
}