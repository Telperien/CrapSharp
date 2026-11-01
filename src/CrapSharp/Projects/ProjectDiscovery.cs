namespace CrapSharp.Projects;

/// <summary>What to analyze: the root folder, the production C# files, and the test projects to run for coverage.</summary>
public sealed record Workspace(string Root, IReadOnlyList<string> SourceFiles, IReadOnlyList<string> TestProjects)
{
    public IReadOnlyDictionary<string, string> SourceProjectByFile { get; init; } =
        new Dictionary<string, string>(PathComparison.Comparer);
}

public static class ProjectDiscovery
{
    private static readonly string[] TestProjectMarkers =
    [
        "Microsoft.NET.Test.Sdk",
        "<IsTestProject>true",
        "Include=\"xunit.v3",
        "Include=\"TUnit",
        "Include=\"NUnit\"",
        "Include=\"MSTest",
    ];

    /// <summary>
    /// A folder (or a .sln/.csproj in it) is searched for .csproj files; test projects are told apart from the rest
    /// and their folders are excluded from the analyzed sources. A lone .cs file is analyzed as is.
    /// </summary>
    public static Workspace Discover(string path)
    {
        var full = Path.GetFullPath(path);
        if (full.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && File.Exists(full))
            return new Workspace(Path.GetDirectoryName(full)!, [full], []);
        var root = Directory.Exists(full) ? full : Path.GetDirectoryName(full)!;
        return FromProjects(root, FindProjects(root));
    }

    public static bool IsTestProject(string projectFileText) =>
        TestProjectMarkers.Any(marker => projectFileText.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static List<string> FindProjects(string root) =>
        Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(project => !SourceDiscovery.IsIgnored(root, project))
            .Order(StringComparer.Ordinal)
            .ToList();

    private static Workspace FromProjects(string root, List<string> projects)
    {
        var tests = projects.Where(p => IsTestProject(File.ReadAllText(p))).ToList();
        var testDirs = tests.Select(p => Path.GetDirectoryName(p)!).ToList();
        var sourceDirs = projects.Except(tests).Select(p => Path.GetDirectoryName(p)!).ToList();
        if (projects.Count == 0)
            sourceDirs.Add(root);
        var files = sourceDirs
            .SelectMany(SourceDiscovery.FindCSharpFiles)
            .Distinct(PathComparison.Comparer)
            .Where(file => !testDirs.Any(dir => IsUnder(dir, file)))
            .Order(StringComparer.Ordinal)
            .ToList();
        var sourceProjectByFile = new Dictionary<string, string>(PathComparison.Comparer);
        foreach (var project in projects.Except(tests))
        {
            var directory = Path.GetDirectoryName(project)!;
            foreach (var file in files.Where(file => IsUnder(directory, file)))
                if (!sourceProjectByFile.TryGetValue(file, out var previous)
                    || directory.Length > Path.GetDirectoryName(previous)!.Length)
                    sourceProjectByFile[file] = project;
        }
        return new Workspace(root, files, tests) { SourceProjectByFile = sourceProjectByFile };
    }

    private static bool IsUnder(string directory, string file) =>
        file.StartsWith(directory + Path.DirectorySeparatorChar, PathComparison.Comparison);
}
