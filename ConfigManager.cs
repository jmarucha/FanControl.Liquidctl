using System;
using System.Collections.Generic;
using System.Linq;
using FanControl.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;

namespace FanControl.Liquidctl;

public static class ConfigManager
{
    private static IConfigurationRoot _configs;
    
    internal static IPluginLogger Logger { get; set; }
 

    public static void Init()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddYamlFile(".config.yaml")
            .SetFileLoadExceptionHandler(context =>
            {
                Logger.Log("Error while loading config file: " + context.Exception.Message);
                return;
            });
        _configs = builder.Build();
    }
    

    public static string GetConfigValue(string key)
    {
        // If there is an environment variable for the key set then return it
        var envVarValue = Environment.GetEnvironmentVariable(key);
        if (envVarValue != null)
            return envVarValue;
        
        _configs?.Reload();
        
        return _configs?.GetSection(key)?.Value;
    }

    public static bool GetConfigBool(string key)
    {
        var value = GetConfigValue(key);
        return bool.TryParse(value, out var result) && result;
    }

    public static int GetConfigInt(string key)
    {
        var value = GetConfigValue(key);
        return int.TryParse(value, out var result) ? result : 0;  // default to 0 if parsing fails
    }

    public static string[] GetConfigArray(string key, char separator = ',')
    {
        var value = GetConfigValue(key);
        return value?.Split(separator) ?? Array.Empty<string>();  // default to empty array if parsing fails
    }

    public static float GetConfigFloat(string key)
    {
        var value = GetConfigValue(key);
        return float.TryParse(value, out var result) ? result : 0.0f; // default to 0.0f if parsing fails
    }

}