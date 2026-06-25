namespace PicPack.App.Models;

public sealed class DistributionProgress
{
    public required int ProcessedCount { get; init; }

    public required int TotalCount { get; init; }

    public required string Message { get; init; }
}
