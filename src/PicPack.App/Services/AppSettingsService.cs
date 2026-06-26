using PicPack.App.Models;
using System.IO;
using System.Text.Json;

namespace PicPack.App.Services;

public sealed class AppSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public AppSettingsService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PicPack",
            "settings.json"))
    {
    }

    public AppSettingsService(string settingsPath)
    {
        _settingsPath = settingsPath;
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_settingsPath);
            return Normalize(JsonSerializer.Deserialize<AppSettings>(json, JsonOptions));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var normalized = Normalize(settings);
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(normalized, JsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Saving user preferences should never stop the main file operation workflow.
        }
    }

    private static AppSettings Normalize(AppSettings? settings)
    {
        if (settings is null)
        {
            return new AppSettings();
        }

        return new AppSettings
        {
            FilesPerFolder = Math.Max(1, settings.FilesPerFolder),
            FolderCount = Math.Max(1, settings.FolderCount)
        };
    }
}
