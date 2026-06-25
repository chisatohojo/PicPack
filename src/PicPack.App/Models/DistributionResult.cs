namespace PicPack.App.Models;

public sealed class DistributionResult
{
    public int TotalFiles { get; set; }

    public int ProcessedFiles { get; set; }

    public int SkippedFiles { get; set; }

    public int CreatedFolderCount { get; set; }

    public bool IsCanceled { get; set; }

    public List<FileProcessError> Errors { get; } = [];
}
