namespace KeepersCompound.Dark;

public class PathUtils
{
    public static string ConvertSeparator(string path)
    {
        return path.Replace('\\', '/');
    }

    public static string AbsJoin(string rootPath, string path)
    {
        return Path.IsPathFullyQualified(path) ? path : Path.Join(rootPath, path);
    }
}