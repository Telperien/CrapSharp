using System.Globalization;
using System.Text;

namespace CrapSharp.Scoring;

public static class ReportFormatter
{
    /// <summary>With a root, methods are located by their path relative to it (forward slashes), not by file name alone.</summary>
    public static string Table(IEnumerable<MethodReport> reports, string? root = null)
    {
        var text = new StringBuilder();
        text.AppendLine($"{"CRAP",8} {"CC",4} {"Cov",6}  Method");
        foreach (var r in reports)
            text.AppendLine($"{Crap(r),8} {r.Method.Complexity,4} {Coverage(r),6}  {Location(r, root)}");
        return text.ToString();
    }

    private static string Crap(MethodReport r) =>
        r.Crap is { } s ? Truncation.Down(s, 2).ToString("0.00", CultureInfo.InvariantCulture) : "-";

    private static string Coverage(MethodReport r) =>
        r.Coverage is { } c ? Truncation.Down(c * 100, 0).ToString("0", CultureInfo.InvariantCulture) + "%" : "-";

    private static string Location(MethodReport r, string? root) =>
        $"{r.Method.FullName}  ({FileLabel(r.Method.FilePath, root)}:{r.Method.StartLine})";

    private static string FileLabel(string file, string? root) =>
        root is null ? Path.GetFileName(file) : Path.GetRelativePath(root, file).Replace('\\', '/');
}
