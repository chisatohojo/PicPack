using PicPack.App.Models;
using PicPack.App.Services;

namespace PicPack.Tests;

public sealed class FolderDistributorServiceTests
{
    [Theory]
    [InlineData(1, 3, 1)]
    [InlineData(2, 3, 1)]
    [InlineData(3, 3, 1)]
    [InlineData(4, 3, 2)]
    [InlineData(5, 3, 2)]
    [InlineData(6, 3, 2)]
    [InlineData(1, 1, 1)]
    [InlineData(2, 1, 2)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    public void CalculateFolderIndex_ReturnsExpectedFolder(int imageIndex, int filesPerFolder, int expectedFolder)
    {
        var actual = FolderDistributorService.CalculateFolderIndex(imageIndex, filesPerFolder);

        Assert.Equal(expectedFolder, actual);
        Assert.Equal(expectedFolder.ToString("000"), FolderDistributorService.FormatFolderName(actual));
    }

    [Fact]
    public async Task DistributeAsync_CopiesImagesIntoNumberedFolders()
    {
        using var workspace = TemporaryWorkspace.Create();
        var input = workspace.CreateDirectory("input");
        var output = workspace.CreateDirectory("output");
        var first = workspace.CreateFile(input, "a.jpg");
        var second = workspace.CreateFile(input, "b.png");
        var third = workspace.CreateFile(input, "c.webp");

        File.SetCreationTimeUtc(first, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        File.SetCreationTimeUtc(second, new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        File.SetCreationTimeUtc(third, new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc));

        var service = new FolderDistributorService();
        var result = await service.DistributeAsync(new DistributionOptions
        {
            InputFolder = input,
            OutputFolder = output,
            FilesPerFolder = 2,
            Mode = DistributionMode.Copy
        });

        Assert.Equal(3, result.ProcessedFiles);
        Assert.Equal(0, result.SkippedFiles);
        Assert.True(File.Exists(Path.Combine(output, "001", "a.jpg")));
        Assert.True(File.Exists(Path.Combine(output, "001", "b.png")));
        Assert.True(File.Exists(Path.Combine(output, "002", "c.webp")));
        Assert.True(File.Exists(first));
    }
}
