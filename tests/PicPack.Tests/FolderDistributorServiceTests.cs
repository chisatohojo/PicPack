using PicPack.App.Models;
using PicPack.App.Services;

namespace PicPack.Tests;

public sealed class FolderDistributorServiceTests
{
    [Theory]
    [InlineData(1, 3, 7, 1)]
    [InlineData(2, 3, 7, 1)]
    [InlineData(3, 3, 7, 1)]
    [InlineData(4, 3, 7, 2)]
    [InlineData(5, 3, 7, 2)]
    [InlineData(6, 3, 7, 2)]
    [InlineData(19, 3, 7, 7)]
    [InlineData(20, 3, 7, 7)]
    [InlineData(21, 3, 7, 7)]
    [InlineData(22, 3, 7, 1)]
    [InlineData(23, 3, 7, 1)]
    [InlineData(24, 3, 7, 1)]
    [InlineData(25, 3, 7, 2)]
    [InlineData(1, 1, 3, 1)]
    [InlineData(2, 1, 3, 2)]
    [InlineData(3, 1, 3, 3)]
    [InlineData(4, 1, 3, 1)]
    [InlineData(10, 10, 2, 1)]
    [InlineData(11, 10, 2, 2)]
    [InlineData(21, 10, 2, 1)]
    public void CalculateFolderIndex_CyclesThroughFixedFolderCount(
        int imageIndex,
        int filesPerFolder,
        int folderCount,
        int expectedFolder)
    {
        var actual = FolderDistributorService.CalculateFolderIndex(imageIndex, filesPerFolder, folderCount);

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
            FolderCount = 7,
            Mode = DistributionMode.Copy
        });

        Assert.Equal(3, result.ProcessedFiles);
        Assert.Equal(0, result.SkippedFiles);
        Assert.True(File.Exists(Path.Combine(output, "001", "a.jpg")));
        Assert.True(File.Exists(Path.Combine(output, "001", "b.png")));
        Assert.True(File.Exists(Path.Combine(output, "002", "c.webp")));
        Assert.True(File.Exists(first));
    }

    [Fact]
    public async Task DistributeAsync_CyclesThroughSevenFoldersInThreeImageBatches()
    {
        using var workspace = TemporaryWorkspace.Create();
        var input = workspace.CreateDirectory("input");
        var output = workspace.CreateDirectory("output");

        for (var index = 1; index <= 27; index++)
        {
            var path = workspace.CreateFile(input, $"image{index:000}.jpg");
            File.SetCreationTimeUtc(path, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(index));
        }

        var service = new FolderDistributorService();
        var result = await service.DistributeAsync(new DistributionOptions
        {
            InputFolder = input,
            OutputFolder = output,
            FilesPerFolder = 3,
            FolderCount = 7,
            Mode = DistributionMode.Copy
        });

        Assert.Equal(27, result.ProcessedFiles);
        AssertFolderContains(output, "001", 1, 2, 3, 22, 23, 24);
        AssertFolderContains(output, "002", 4, 5, 6, 25, 26, 27);
        AssertFolderContains(output, "003", 7, 8, 9);
        AssertFolderContains(output, "004", 10, 11, 12);
        AssertFolderContains(output, "005", 13, 14, 15);
        AssertFolderContains(output, "006", 16, 17, 18);
        AssertFolderContains(output, "007", 19, 20, 21);
        Assert.False(Directory.Exists(Path.Combine(output, "008")));
    }

    private static void AssertFolderContains(string output, string folderName, params int[] imageIndexes)
    {
        var actual = Directory
            .EnumerateFiles(Path.Combine(output, folderName))
            .Select(Path.GetFileName)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var expected = imageIndexes.Select(index => $"image{index:000}.jpg").ToArray();

        Assert.Equal(expected, actual);
    }
}
