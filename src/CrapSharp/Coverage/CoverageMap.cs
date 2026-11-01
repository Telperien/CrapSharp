namespace CrapSharp.Coverage;

/// <summary>
/// Line hits per source file, merged from one or more coverage reports.
/// Coverage of a member is computed from the lines inside its line range, so it does not depend on
/// how the compiler named the member (async state machines, closures...).
/// </summary>
public sealed class CoverageMap
{
    private readonly Dictionary<string, Dictionary<int, int>> _files = new(PathComparison.Comparer);

    public int FileCount => _files.Count;

    /// <summary>True when the reports mention this file at all.</summary>
    public bool Contains(string file) => _files.ContainsKey(Path.GetFullPath(file));

    /// <summary>Records the hits of a line. A line seen several times keeps its highest hit count.</summary>
    public void Record(string file, int line, int hits)
    {
        var key = Path.GetFullPath(file);
        if (!_files.TryGetValue(key, out var lines))
            _files[key] = lines = [];
        lines[line] = Math.Max(hits, lines.GetValueOrDefault(line));
    }

    public void Merge(CoverageMap other)
    {
        foreach (var (file, lines) in other._files)
            foreach (var (line, hits) in lines)
                Record(file, line, hits);
    }

    /// <summary>
    /// Fraction (0..1) of the reported lines in [startLine, endLine] that were hit.
    /// A member with no reported line counts as 0, like crapper does for a function missing from the report.
    /// </summary>
    public double CoverageOf(string file, int startLine, int endLine)
    {
        if (!_files.TryGetValue(Path.GetFullPath(file), out var lines))
            return 0;
        var inRange = lines.Where(l => l.Key >= startLine && l.Key <= endLine).ToList();
        return inRange.Count == 0 ? 0 : (double)inRange.Count(l => l.Value > 0) / inRange.Count;
    }
}
