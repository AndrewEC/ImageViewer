namespace ImageViewer.Core.Config;

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using ImageViewer.Core.Models;
using ImageViewer.Core.Utils;

#pragma warning disable CA1852
[JsonSerializable(typeof(Config))]
[JsonSerializable(typeof(SortMethod))]
[JsonSerializable(typeof(int))]
internal partial class ConfigContext : JsonSerializerContext
{
}
#pragma warning restore CA1852

public class ConfigState
{

    public static readonly ConfigState Instance = new();
    
    private static readonly JsonSerializerOptions ConfigSerializerOptions = new()
    {
        TypeInfoResolver = ConfigContext.Default
    };
    private static readonly Lock SyncLock = new();

    private static readonly string AppName = "ImageViewer";
    private static readonly string ConfigFileName = "config.json";

    private readonly ConsoleLogger<ConfigState> logger = new();
    private Config? currentConfig;
    private PathLike? configFilePath;

    private ConfigState() { }

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

            PathLike configPath = CreateAndGetConfigFilePath();
            logger.Log($"Using config path: [{configPath}]");

            try
            {
                string configJson = File.ReadAllText(configPath.PathString);
                currentConfig = JsonSerializer.Deserialize<Config>(configJson, ConfigSerializerOptions)!;
            }
            catch (Exception e)
            {
                logger.Log($"Failed to load configuration from JSON file. Cause: [{e}].");
                currentConfig = new Config();
            }

            return new Config(currentConfig);
        }
    }

    public void SaveConfig(Config nextConfig)
    {
        lock (SyncLock)
        {
            PathLike configPath = CreateAndGetConfigFilePath();
            logger.Log($"Saving config to: [{configPath}]");

            string configJson = JsonSerializer.Serialize(nextConfig, ConfigSerializerOptions);
            File.WriteAllText(configPath.PathString, configJson);

            logger.Log($"Updated configuration: [{configJson}]");
            currentConfig = new Config(nextConfig);
        }
    }

    public PathLike CreateAndGetConfigFilePath()
    {
        lock (SyncLock)
        {
            logger.Log("Getting config file path...");
            if (configFilePath != null)
            {
                return configFilePath;
            }

            string appDataRoamingPath = Environment
                .GetFolderPath(Environment.SpecialFolder.ApplicationData);

            PathLike configDataFolder = new PathLike(appDataRoamingPath).Join(AppName);
            if (!configDataFolder.IsDirectory())
            {
                logger.Log($"Config data folder not found. Creating folder at: [{configDataFolder}]");
                Directory.CreateDirectory(configDataFolder.PathString);
            }

            PathLike configFile = configDataFolder.Join(ConfigFileName);
            if (!configFile.IsFile())
            {
                logger.Log($"Config file not found. Creating default config at: [{configFile}]");
                Config defaultConfig = new();
                string defaultConfigJson = JsonSerializer.Serialize(defaultConfig, ConfigSerializerOptions);
                logger.Log($"Writing default config: [{defaultConfigJson}]");
                File.WriteAllText(configFile.PathString, defaultConfigJson);
            }

            configFilePath = configFile;
            return configFile;
        }
    }
}