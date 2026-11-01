namespace CrapSharp.Coverage;

/// <summary>Locates Cobertura reports on disk (coverlet writes TestResults/&lt;guid&gt;/coverage.cobertura.xml).</summary>
public static class CoverageReportFinder
{
    private const string Pattern = "*cobertura*.xml";

    /// <summary>
    /// Newest report of each test run. A run is the folder holding the guid folder, so old guid folders
    /// left in TestResults by earlier runs are not counted twice.
    /// </summary>
    public static IReadOnlyList<string> NewestPerRun(string root) =>
        Find(root)
            .GroupBy(report => Path.GetDirectoryName(Path.GetDirectoryName(report)) ?? "", PathComparison.Comparer)
            .Select(run => run.OrderByDescending(File.GetLastWriteTimeUtc).First())
            .Order(StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<string> ModifiedSince(string root, DateTime sinceUtc) =>
        Find(root).Where(report => File.GetLastWriteTimeUtc(report) >= sinceUtc).Order(StringComparer.Ordinal).ToList();

    private static IEnumerable<string> Find(string root) =>
        Directory.Exists(root)
            ? Directory.EnumerateFiles(root, Pattern, SearchOption.AllDirectories)
                .Where(report => !SourceDiscovery.IsIgnored(root, report))
            : [];
}
