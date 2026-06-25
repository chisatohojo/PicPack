namespace PicPack.App.Models;

public sealed class DistributionOptions
{
    public required string InputFolder { get; init; }

    public required string OutputFolder { get; init; }

    public required int FilesPerFolder { get; init; }

    public required DistributionMode Mode { get; init; }
}
