namespace ImageViewer.State;

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using ImageViewer.Models;
using ImageViewer.Utils;

#pragma warning disable CA1852
[JsonSerializable(typeof(Config))]
[JsonSerializable(typeof(SortMethod))]
[JsonSerializable(typeof(int))]
internal partial class ConfigContext : JsonSerializerContext
{
}
#pragma warning restore CA1852

public interface IConfigState
{
    Config LoadConfig();
    bool SaveConfig(Config nextConfig);
    PathLike GetConfigFilePath();
}

public class ConfigState : IConfigState
{   
    private static readonly JsonSerializerOptions ConfigSerializerOptions = new()
    {
        TypeInfoResolver = ConfigContext.Default
    };
    private static readonly Lock SyncLock = new();

    private static readonly string AppName = "ImageViewer";
    private static readonly string ConfigFileName = "config.json";

    private readonly ConsoleLogger<ConfigState> logger = new();
    private Config? currentConfig;

    public Config LoadConfig()
    {
        lock (SyncLock)
        {
            logger.Log("Loading config...");
            if (currentConfig != null)
            {
                logger.Log("Config was previously loaded. Returning cached config values.");
                return new Config(currentConfig);
            }

            PathLike configPath = GetConfigFilePath();
            logger.Log($"Using config path: [{configPath}]");
            Config loaded = DoLoadConfig(configPath);
            currentConfig = loaded;

            return new Config(currentConfig);
        }
    }

    private Config DoLoadConfig(PathLike configFilePath)
    {
        if (!configFilePath.IsFile())
        {
            logger.Log("Config file not found. Using default config.");
            return new Config();
        }

        try
        {
            string configJson = File.ReadAllText(configFilePath.PathString);
            logger.Log($"Loading config: [{configJson}].");
            return JsonSerializer.Deserialize<Config>(configJson, ConfigSerializerOptions)!;
        }
        catch (Exception e)
        {
            logger.Error("Failed to load configuration from JSON file.", e);
            return new Config();
        }
    }

    public bool SaveConfig(Config nextConfig)
    {
        lock (SyncLock)
        {
            logger.Log("Saving config...");
            PathLike configPath = GetConfigFilePath();
            if (!configPath.Parent().CreateDirectory())
            {
                return false;
            }

            try
            {
                logger.Log($"Saving config to: [{configPath}].");
                string defaultConfigJson = JsonSerializer.Serialize(nextConfig, ConfigSerializerOptions);
                logger.Log($"Writing config: [{defaultConfigJson}].");
                File.WriteAllText(configPath.PathString, defaultConfigJson);
                currentConfig = nextConfig;
                return true;
            }
            catch (Exception e)
            {
                logger.Error("Failed to save config.", e);
                return false;
            }
        }
    }

    public PathLike GetConfigFilePath()
    {
        string appDataRoamingPath = Environment
            .GetFolderPath(Environment.SpecialFolder.ApplicationData);

        return new PathLike(appDataRoamingPath).Join(AppName).Join(ConfigFileName);
    }
}