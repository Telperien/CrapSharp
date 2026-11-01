namespace CrapSharp.Coverage;

/// <summary>Locates Cobertura reports on disk (coverlet writes TestResults/&lt;guid&gt;/coverage.cobertura.xml).</summary>
public static class CoverageReportFinder
{
    private const string Pattern = "*cobertura*.xml";

    /// <summary>How far before the newest report of a run another report may be written and still belong to it.</summary>
    private static readonly TimeSpan RunSpan = TimeSpan.FromMinutes(10);

    /// <summary>
    /// The reports of the latest test run of each folder. A run is the folder holding the guid folders, so guid
    /// folders left by earlier runs are not counted. One `dotnet test` of a multi-target project writes one report
    /// per target framework, a few seconds or minutes apart: those all belong to the run and are all kept.
    /// </summary>
    public static IReadOnlyList<string> LatestRuns(string root) =>
        Find(root)
            .GroupBy(report => Path.GetDirectoryName(Path.GetDirectoryName(report)) ?? "", PathComparison.Comparer)
            .SelectMany(KeepLatestRun)
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>Every report under a folder that only this tool wrote (it was emptied before the run).</summary>
    public static IReadOnlyList<string> All(string root) => Find(root).Order(StringComparer.Ordinal).ToList();

    private static IEnumerable<string> KeepLatestRun(IEnumerable<string> reports)
    {
        var list = reports.ToList();
        var newest = list.Max(File.GetLastWriteTimeUtc);
        return list.Where(report => newest - File.GetLastWriteTimeUtc(report) <= RunSpan);
    }

    public static IReadOnlyList<string> ModifiedSince(string root, DateTime sinceUtc) =>
        Find(root).Where(report => File.GetLastWriteTimeUtc(report) >= sinceUtc).Order(StringComparer.Ordinal).ToList();

    private static IEnumerable<string> Find(string root) =>
        Directory.Exists(root)
            ? Directory.EnumerateFiles(root, Pattern, SearchOption.AllDirectories)
                .Where(report => !SourceDiscovery.IsIgnored(root, report))
            : [];
}
