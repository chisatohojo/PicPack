using System.IO;

namespace PicPack.App.Services;

public sealed class SafeFileNameService
{
    public string GetAvailablePath(string directoryPath, string fileName)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ArgumentException("出力フォルダを指定してください。", nameof(directoryPath));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("ファイル名を指定してください。", nameof(fileName));
        }

        var firstPath = Path.Combine(directoryPath, fileName);
        if (!File.Exists(firstPath))
        {
            return firstPath;
        }

        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        for (var index = 1; index < int.MaxValue; index++)
        {
            var candidate = Path.Combine(directoryPath, $"{name}_{index:000}{extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new IOException($"利用可能なファイル名を作成できませんでした: {fileName}");
    }
}
