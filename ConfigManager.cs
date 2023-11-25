using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;
using YamlDotNet.RepresentationModel;

// ReSharper disable once IdentifierTypo
namespace FanControl.Liquidctl;

/// <summary>
/// A configuration manager with yaml config file support. The manager expects to read ".config.yaml" file from the
/// current execution directory. If it does not find it, it creates one by default.
/// </summary>
public static class ConfigManager
{
    private static IConfigurationRoot _configs;

    private const string YamlFileName = "config.yaml";

    /// <summary>
    /// Set this property  if you want to handle any exception that occurs when the config file is read.
    /// Otherwise the thrown exception should be handled in the calling code.
    /// </summary>
    internal static Action<FileLoadExceptionContext> FileLoadExceptionHandler { get; set; }


    private static void CreateYamlIfNotPresent()
    {
        if (File.Exists($"{Environment.CurrentDirectory}\\{YamlFileName}")) return;
        var appMapping = new YamlMappingNode
        {
            {
                "liquidCtl",
                // ReSharper disable once StringLiteralTypo
                new YamlMappingNode { { "execPath", $"{Environment.CurrentDirectory}\\liquidctl.exe" } }
            },
            { "debug", "true" }
        };
        var document = new YamlDocument(new YamlMappingNode { { "app", appMapping } });
        var yaml = new YamlStream(document);

        // Save the stream to a file
        using var writer = new StreamWriter($"{Environment.CurrentDirectory}\\{YamlFileName}");
        writer.AutoFlush = true;
        yaml.Save(writer);
    }

    /// <summary>
    /// Initializes the Config Manager and make is ready for use. This method should be called only once in a programs lifecycle.
    /// It will automatically reload the configuration if the underlying config file is edited 
    /// </summary>
    public static void Init()
    {
        CreateYamlIfNotPresent();
        if (FileLoadExceptionHandler != null)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddYamlFile(YamlFileName)
                .SetFileLoadExceptionHandler(context => FileLoadExceptionHandler(context));
            _configs = builder.Build();
        }
        else
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddYamlFile(YamlFileName);
            _configs = builder.Build();
        }
    }

    /// <summary>
    /// Gets the configuration property for the key.
    /// </summary>
    /// <param name="key">The key can be in the format "x.y.z" for nested properties</param>
    /// <returns>The string value stored else empty string</returns>
    public static string GetConfigValue(string key)
    {
        var envKey = key.Replace('.','_').ToUpper();
        // If there is an environment variable for the key set then return it
        var envVarValue = Environment.GetEnvironmentVariable(envKey);
        if (envVarValue != null)
            return envVarValue;

        _configs?.Reload();
        var yamlPath = key.Replace('.', ':'); 
        return _configs?.GetSection(yamlPath).Value;
    }

    /// <summary>
    ///  Gets the configuration property for the key.
    /// </summary>
    /// <param name="key">The key can be in the format "x.y.z" for nested properties</param>
    /// <returns>The boolean value stored else a false if not parseable</returns>
    public static bool GetConfigBool(string key)
    {
        var value = GetConfigValue(key);
        return bool.TryParse(value, out var result) && result;
    }

    /// <summary>
    /// Gets the configuration property for the key. 
    /// </summary>
    /// <param name="key">The key can be in the format "x.y.z" for nested properties</param>
    /// <returns>The integer value stored else 0 if not parseable</returns>
    public static int GetConfigInt(string key)
    {
        var value = GetConfigValue(key);
        return int.TryParse(value, out var result) ? result : 0; // default to 0 if parsing fails
    }

    /// <summary>
    /// Gets the configuration property for the key.
    /// </summary>
    /// <param name="key">The key can be in the format "x.y.z" for nested properties</param>
    /// <param name="separator">The delimiter to use when parsing the string value</param>
    /// <returns>The array of strings stored else an empty array if not parseable</returns>
    public static string[] GetConfigArray(string key, char separator = ',')
    {
        var value = GetConfigValue(key);
        return value?.Split(separator) ?? Array.Empty<string>(); // default to empty array if parsing fails
    }

    /// <summary>
    /// Gets the configuration property for the key.
    /// </summary>
    /// <param name="key">The key can be in the format "x.y.z" for nested properties</param>
    /// <returns>The float value stored else 0.0f if not parseable</returns>
    public static float GetConfigFloat(string key)
    {
        var value = GetConfigValue(key);
        return float.TryParse(value, out var result) ? result : 0.0f; // default to 0.0f if parsing fails
    }
}