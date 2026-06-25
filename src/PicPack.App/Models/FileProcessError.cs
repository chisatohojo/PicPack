namespace PicPack.App.Models;

public sealed class FileProcessError
{
    public required string SourcePath { get; init; }

    public required string Message { get; init; }
}
