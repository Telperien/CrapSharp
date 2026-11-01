using CrapSharp.Complexity;
using CrapSharp.Coverage;

namespace CrapSharp.Scoring;

/// <summary>One line of the report. Coverage and Crap are null when no coverage data was supplied.</summary>
public sealed record MethodReport(MethodComplexity Method, double? Coverage, double? Crap)
{
    public static IReadOnlyList<MethodReport> Build(IEnumerable<MethodComplexity> methods, CoverageMap? coverage) =>
        methods.Select(m => For(m, coverage))
            .OrderByDescending(r => r.Crap ?? r.Method.Complexity)
            .ThenBy(r => r.Method.FullName, StringComparer.Ordinal)
            .ToList();

    private static MethodReport For(MethodComplexity method, CoverageMap? coverage)
    {
        if (coverage is null)
            return new MethodReport(method, null, null);
        var covered = coverage.CoverageOf(method.FilePath, method.StartLine, method.EndLine);
        return new MethodReport(method, covered, CrapCalculator.Score(method.Complexity, covered));
    }
}
