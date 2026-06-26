using PicPack.App.Models;
using PicPack.App.Services;

namespace PicPack.Tests;

public sealed class AppSettingsServiceTests
{
    [Fact]
    public void Load_ReturnsDefaultsWhenSettingsFileDoesNotExist()
    {
        using var workspace = TemporaryWorkspace.Create();
        var directory = workspace.CreateDirectory("settings");
        var service = new AppSettingsService(Path.Combine(directory, "settings.json"));

        var actual = service.Load();

        Assert.Equal(3, actual.FilesPerFolder);
        Assert.Equal(7, actual.FolderCount);
    }

    [Fact]
    public void Save_AndLoad_RoundTripsNumericSettings()
    {
        using var workspace = TemporaryWorkspace.Create();
        var directory = workspace.CreateDirectory("settings");
        var settingsPath = Path.Combine(directory, "settings.json");
        var service = new AppSettingsService(settingsPath);

        service.Save(new AppSettings
        {
            FilesPerFolder = 9,
            FolderCount = 4
        });

        var actual = service.Load();

        Assert.Equal(9, actual.FilesPerFolder);
        Assert.Equal(4, actual.FolderCount);
    }

    [Fact]
    public void Load_ClampsInvalidNumbersToMinimum()
    {
        using var workspace = TemporaryWorkspace.Create();
        var directory = workspace.CreateDirectory("settings");
        var settingsPath = Path.Combine(directory, "settings.json");
        File.WriteAllText(settingsPath, """{"FilesPerFolder":0,"FolderCount":-5}""");
        var service = new AppSettingsService(settingsPath);

        var actual = service.Load();

        Assert.Equal(1, actual.FilesPerFolder);
        Assert.Equal(1, actual.FolderCount);
    }
}
