namespace CrapSharp;

/// <summary>Finds the C# files to analyze, skipping build output and generated code.</summary>
public static class SourceDiscovery
{
    private static readonly HashSet<string> IgnoredDirectories =
        new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", ".git", "node_modules" };

    private static readonly string[] GeneratedSuffixes = [".g.cs", ".generated.cs", ".designer.cs"];

    public static IEnumerable<string> FindCSharpFiles(string path)
    {
        if (File.Exists(path))
            return [path];
        return Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories)
            .Where(file => !IsIgnored(path, file))
            .Order(StringComparer.Ordinal);
    }

    /// <summary>True when the file sits in a build-output directory (bin, obj...) below root, or is generated code.</summary>
    public static bool IsIgnored(string root, string file)
    {
        var relative = Path.GetRelativePath(root, file);
        var directories = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[..^1];
        return directories.Any(IgnoredDirectories.Contains)
            || GeneratedSuffixes.Any(suffix => file.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }
}
