namespace PicPack.Tests;

internal sealed class TemporaryWorkspace : IDisposable
{
    private readonly string _rootPath;

    private TemporaryWorkspace(string rootPath)
    {
        _rootPath = rootPath;
    }

    public static TemporaryWorkspace Create()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "PicPack.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(rootPath);
        return new TemporaryWorkspace(rootPath);
    }

    public string CreateDirectory(string name)
    {
        var path = Path.Combine(_rootPath, name);
        Directory.CreateDirectory(path);
        return path;
    }

    public string CreateFile(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, "test");
        return path;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_rootPath))
            {
                Directory.Delete(_rootPath, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
