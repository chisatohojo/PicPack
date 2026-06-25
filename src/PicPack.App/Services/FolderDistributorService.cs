using PicPack.App.Models;
using System.IO;

namespace PicPack.App.Services;

public sealed class FolderDistributorService
{
    private readonly ImageFileFinder _imageFileFinder;
    private readonly SafeFileNameService _safeFileNameService;

    public FolderDistributorService()
        : this(new ImageFileFinder(), new SafeFileNameService())
    {
    }

    public FolderDistributorService(ImageFileFinder imageFileFinder, SafeFileNameService safeFileNameService)
    {
        _imageFileFinder = imageFileFinder;
        _safeFileNameService = safeFileNameService;
    }

    public Task<DistributionResult> DistributeAsync(
        DistributionOptions options,
        IProgress<DistributionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateOptions(options);

        return Task.Run(() => Distribute(options, progress, cancellationToken));
    }

    public int CountTargetImages(string inputFolder)
    {
        return _imageFileFinder.FindImages(inputFolder).Count;
    }

    public static int CalculateFolderIndex(int imageIndex, int filesPerFolder)
    {
        if (imageIndex < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(imageIndex), "画像番号は1以上で指定してください。");
        }

        if (filesPerFolder < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(filesPerFolder), "1フォルダあたりの枚数は1以上で指定してください。");
        }

        return ((imageIndex - 1) / filesPerFolder) + 1;
    }

    public static string FormatFolderName(int folderIndex)
    {
        if (folderIndex < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(folderIndex), "フォルダ番号は1以上で指定してください。");
        }

        return folderIndex.ToString("000");
    }

    private DistributionResult Distribute(
        DistributionOptions options,
        IProgress<DistributionProgress>? progress,
        CancellationToken cancellationToken)
    {
        var images = _imageFileFinder.FindImages(options.InputFolder);
        var result = new DistributionResult
        {
            TotalFiles = images.Count
        };

        var createdFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Report(progress, result, $"対象画像枚数: {images.Count}");

        for (var index = 0; index < images.Count; index++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                result.IsCanceled = true;
                Report(progress, result, "キャンセルされました");
                break;
            }

            var image = images[index];
            var imageIndex = index + 1;
            var folderIndex = CalculateFolderIndex(imageIndex, options.FilesPerFolder);
            var folderName = FormatFolderName(folderIndex);
            var destinationFolder = Path.Combine(options.OutputFolder, folderName);

            try
            {
                if (!Directory.Exists(destinationFolder))
                {
                    Directory.CreateDirectory(destinationFolder);
                    if (createdFolders.Add(destinationFolder))
                    {
                        result.CreatedFolderCount++;
                        Report(progress, result, $"出力フォルダ作成: {folderName}");
                    }
                }

                var destinationPath = _safeFileNameService.GetAvailablePath(destinationFolder, image.Name);
                ProcessFile(image, destinationPath, options.Mode);
                result.ProcessedFiles++;

                var operationLabel = options.Mode == DistributionMode.Copy ? "コピー" : "移動";
                Report(progress, result, $"{operationLabel}: {image.Name} -> {folderName}\\{Path.GetFileName(destinationPath)}");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or PathTooLongException)
            {
                result.SkippedFiles++;
                result.Errors.Add(new FileProcessError
                {
                    SourcePath = image.FullName,
                    Message = exception.Message
                });
                Report(progress, result, $"スキップ: {image.Name} ({exception.Message})");
            }
        }

        if (!result.IsCanceled)
        {
            Report(progress, result, $"完了: 成功 {result.ProcessedFiles} 件 / スキップ {result.SkippedFiles} 件");
        }

        return result;
    }

    private static void ProcessFile(FileInfo sourceFile, string destinationPath, DistributionMode mode)
    {
        var creationTimeUtc = sourceFile.CreationTimeUtc;
        var lastWriteTimeUtc = sourceFile.LastWriteTimeUtc;

        if (mode == DistributionMode.Copy)
        {
            File.Copy(sourceFile.FullName, destinationPath, overwrite: false);
        }
        else
        {
            File.Move(sourceFile.FullName, destinationPath);
        }

        TryPreserveTimestamps(destinationPath, creationTimeUtc, lastWriteTimeUtc);
    }

    private static void TryPreserveTimestamps(string destinationPath, DateTime creationTimeUtc, DateTime lastWriteTimeUtc)
    {
        try
        {
            File.SetCreationTimeUtc(destinationPath, creationTimeUtc);
            File.SetLastWriteTimeUtc(destinationPath, lastWriteTimeUtc);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentOutOfRangeException)
        {
            // Timestamp preservation is best-effort; the file operation itself has already succeeded.
        }
    }

    private static void ValidateOptions(DistributionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.InputFolder))
        {
            throw new ArgumentException("入力フォルダを指定してください。", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.OutputFolder))
        {
            throw new ArgumentException("出力フォルダを指定してください。", nameof(options));
        }

        if (options.FilesPerFolder < 1)
        {
            throw new ArgumentException("1フォルダあたりの枚数は1以上で指定してください。", nameof(options));
        }
    }

    private static void Report(IProgress<DistributionProgress>? progress, DistributionResult result, string message)
    {
        progress?.Report(new DistributionProgress
        {
            ProcessedCount = result.ProcessedFiles + result.SkippedFiles,
            TotalCount = result.TotalFiles,
            Message = message
        });
    }
}
