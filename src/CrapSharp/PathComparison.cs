namespace CrapSharp;

/// <summary>File paths are case-insensitive on Windows only.</summary>
public static class PathComparison
{
    public static readonly StringComparison Comparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static readonly StringComparer Comparer =
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
}
