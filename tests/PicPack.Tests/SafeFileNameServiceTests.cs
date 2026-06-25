using PicPack.App.Services;

namespace PicPack.Tests;

public sealed class SafeFileNameServiceTests
{
    [Fact]
    public void GetAvailablePath_AppendsSequentialSuffixWhenFileExists()
    {
        using var workspace = TemporaryWorkspace.Create();
        var directory = workspace.CreateDirectory("output");
        workspace.CreateFile(directory, "image.png");

        var service = new SafeFileNameService();

        var actual = service.GetAvailablePath(directory, "image.png");

        Assert.Equal(Path.Combine(directory, "image_001.png"), actual);
    }

    [Fact]
    public void GetAvailablePath_IncrementsSuffixUntilPathIsAvailable()
    {
        using var workspace = TemporaryWorkspace.Create();
        var directory = workspace.CreateDirectory("output");
        workspace.CreateFile(directory, "image.png");
        workspace.CreateFile(directory, "image_001.png");

        var service = new SafeFileNameService();

        var actual = service.GetAvailablePath(directory, "image.png");

        Assert.Equal(Path.Combine(directory, "image_002.png"), actual);
    }
}
