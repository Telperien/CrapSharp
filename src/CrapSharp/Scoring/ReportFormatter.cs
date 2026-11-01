using System.Globalization;
using System.Text;

namespace CrapSharp.Scoring;

public static class ReportFormatter
{
    public static string Table(IEnumerable<MethodReport> reports)
    {
        var text = new StringBuilder();
        text.AppendLine($"{"CRAP",8} {"CC",4} {"Cov",6}  Method");
        foreach (var r in reports)
            text.AppendLine($"{Crap(r),8} {r.Method.Complexity,4} {Coverage(r),6}  {Location(r)}");
        return text.ToString();
    }

    private static string Crap(MethodReport r) =>
        r.Crap is { } s ? Truncation.Down(s, 2).ToString("0.00", CultureInfo.InvariantCulture) : "-";

    private static string Coverage(MethodReport r) =>
        r.Coverage is { } c ? Truncation.Down(c * 100, 0).ToString("0", CultureInfo.InvariantCulture) + "%" : "-";

    private static string Location(MethodReport r) =>
        $"{r.Method.FullName}  ({Path.GetFileName(r.Method.FilePath)}:{r.Method.StartLine})";
}
