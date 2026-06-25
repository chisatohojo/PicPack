using PicPack.App.Services;

namespace PicPack.Tests;

public sealed class ImageFileFinderTests
{
    [Theory]
    [InlineData("photo.jpg", true)]
    [InlineData("photo.jpeg", true)]
    [InlineData("photo.png", true)]
    [InlineData("photo.webp", true)]
    [InlineData("photo.bmp", true)]
    [InlineData("memo.txt", false)]
    [InlineData("design.psd", false)]
    [InlineData("archive.zip", false)]
    public void IsSupportedImageFile_ReturnsExpectedResult(string fileName, bool expected)
    {
        Assert.Equal(expected, ImageFileFinder.IsSupportedImageFile(fileName));
    }

    [Fact]
    public void FindImages_SortsByCreationTimeThenFileName()
    {
        using var workspace = TemporaryWorkspace.Create();
        var input = workspace.CreateDirectory("input");
        var second = workspace.CreateFile(input, "b.png");
        var first = workspace.CreateFile(input, "a.jpg");
        var third = workspace.CreateFile(input, "c.bmp");
        workspace.CreateFile(input, "ignored.txt");

        var sharedTime = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        File.SetCreationTimeUtc(second, sharedTime);
        File.SetCreationTimeUtc(first, sharedTime);
        File.SetCreationTimeUtc(third, new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc));

        var finder = new ImageFileFinder();

        var actual = finder.FindImages(input).Select(file => file.Name).ToArray();

        Assert.Equal(["a.jpg", "b.png", "c.bmp"], actual);
    }
}
