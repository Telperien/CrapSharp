using System.Text.Json;

namespace CrapSharp.Scoring;

/// <summary>Writes the report as JSON (crapper writes EDN; JSON is the natural choice in .NET).</summary>
public static class JsonReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static void Write(IEnumerable<MethodReport> reports, string path, ReportContext? context = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, Serialize(reports, context));
    }

    /// <summary>
    /// Coverage is a percentage (0-100) like in crapper's file; both values are null without coverage data.
    /// Values are truncated, not rounded, like the table (see <see cref="Truncation"/>); crapRaw is the exact
    /// score the threshold verdict compares. With a context, the scope and the coverage origin are stated too.
    /// </summary>
    public static string Serialize(IEnumerable<MethodReport> reports, ReportContext? context = null) =>
        JsonSerializer.Serialize(
            new { scope = context is null ? null : ToScope(context), coverage = context is null ? null : ToCoverage(context), entries = reports.Select(ToEntry) },
            JsonOptions);

    private static object ToScope(ReportContext c) =>
        new { complete = c.Complete, changed = c.Changed, changedSince = c.ChangedSince, filter = c.Filter, exclude = c.Excluded, files = c.FilesAnalyzed };

    private static object ToCoverage(ReportContext c) =>
        new { source = c.CoverageSource.ToString(), reports = c.CoverageReports };

    private static object ToEntry(MethodReport r) => new
    {
        name = r.Method.FullName,
        file = r.Method.FilePath,
        startLine = r.Method.StartLine,
        endLine = r.Method.EndLine,
        complexity = r.Method.Complexity,
        coverage = r.Coverage is { } c ? Truncation.Down(c * 100, 1) : (double?)null,
        crap = r.Crap is { } s ? Truncation.Down(s, 2) : (double?)null,
        crapRaw = r.Crap,
    };
}
