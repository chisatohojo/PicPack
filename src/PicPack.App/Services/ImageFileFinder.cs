using System.IO;

namespace PicPack.App.Services;

public sealed class ImageFileFinder
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".bmp"
    };

    public IReadOnlyList<FileInfo> FindImages(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("入力フォルダを指定してください。", nameof(folderPath));
        }

        var directory = new DirectoryInfo(folderPath);
        if (!directory.Exists)
        {
            throw new DirectoryNotFoundException($"入力フォルダが見つかりません: {folderPath}");
        }

        return directory
            .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
            .Where(file => IsSupportedImageFile(file.FullName))
            .OrderBy(file => file.CreationTimeUtc)
            .ThenBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool IsSupportedImageFile(string filePath)
    {
        return SupportedExtensions.Contains(Path.GetExtension(filePath));
    }
}
