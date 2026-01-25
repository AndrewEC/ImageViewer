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

            PathLike? configPath = CreateAndGetConfigFilePath();
            if (configPath == null)
            {
                logger.Log("Could not create config data path. Falling back to default config.");
                return new Config();
            }

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
            PathLike? configPath = CreateAndGetConfigFilePath();
            if (configPath == null)
            {
                return;
            }

            logger.Log($"Saving config to: [{configPath}]");

            try
            {
                string configJson = JsonSerializer.Serialize(nextConfig, ConfigSerializerOptions);
                logger.Log($"Saving updated configuration: [{configJson}]");
                File.WriteAllText(configPath.PathString, configJson);
            }
            catch (Exception e)
            {
                logger.Log($"Failed to save configuration changes. Cause: [{e.Message}].");
                return;
            }

            currentConfig = new Config(nextConfig);
        }
    }

    public PathLike? CreateAndGetConfigFilePath()
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
                try
                {
                    logger.Log($"Config data folder not found. Creating folder at: [{configDataFolder}]");
                    Directory.CreateDirectory(configDataFolder.PathString);
                }
                catch (Exception e)
                {
                    logger.Log($"Failed to create data directory. Cause: [{e.Message}].");
                    return null;
                }
            }

            PathLike configFile = configDataFolder.Join(ConfigFileName);
            if (!configFile.IsFile())
            {
                try
                {
                    logger.Log($"Config file not found. Creating default config at: [{configFile}]");
                    Config defaultConfig = new();
                    string defaultConfigJson = JsonSerializer.Serialize(defaultConfig, ConfigSerializerOptions);
                    logger.Log($"Writing default config: [{defaultConfigJson}]");
                    File.WriteAllText(configFile.PathString, defaultConfigJson);
                }
                catch (Exception e)
                {
                    logger.Log($"Failed to create default config file. Cause: [{e.Message}].");
                    return null;
                }
            }

            configFilePath = configFile;
            return configFile;
        }
    }
}