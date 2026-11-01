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

    public static void Write(IEnumerable<MethodReport> reports, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, Serialize(reports));
    }

    /// <summary>
    /// Coverage is a percentage (0-100) like in crapper's file; both values are null without coverage data.
    /// Values are truncated, not rounded, like the table (see <see cref="Truncation"/>).
    /// </summary>
    public static string Serialize(IEnumerable<MethodReport> reports) =>
        JsonSerializer.Serialize(new { entries = reports.Select(ToEntry) }, JsonOptions);

    private static object ToEntry(MethodReport r) => new
    {
        name = r.Method.FullName,
        file = r.Method.FilePath,
        startLine = r.Method.StartLine,
        endLine = r.Method.EndLine,
        complexity = r.Method.Complexity,
        coverage = r.Coverage is { } c ? Truncation.Down(c * 100, 1) : (double?)null,
        crap = r.Crap is { } s ? Truncation.Down(s, 2) : (double?)null,
    };
}
