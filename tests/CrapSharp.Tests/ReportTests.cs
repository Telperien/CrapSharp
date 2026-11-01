using CrapSharp.Complexity;
using CrapSharp.Coverage;
using CrapSharp.Scoring;
using Xunit;

namespace CrapSharp.Tests;

public class ReportTests
{
    private static readonly string File1 = Path.GetFullPath("Foo.cs");

    private static MethodComplexity Method(string name, int cc, int start, int end) =>
        new(name, File1, start, end, cc);

    [Fact]
    public void WithoutCoverageScoresAreNullAndSortedByComplexity()
    {
        var reports = MethodReport.Build([Method("A", 2, 1, 2), Method("B", 7, 3, 9)], null);
        Assert.Equal(["B", "A"], reports.Select(r => r.Method.FullName));
        Assert.All(reports, r => Assert.Null(r.Crap));
    }

    [Fact]
    public void WorstCrapComesFirstAndUntestedCodeIsPenalized()
    {
        var coverage = new CoverageMap();
        coverage.Record(File1, 1, 1);   // Tested: lines 1-2 covered
        coverage.Record(File1, 2, 1);
        coverage.Record(File1, 5, 0);   // Untested: line 5 never hit
        var reports = MethodReport.Build([Method("Tested", 5, 1, 2), Method("Untested", 3, 5, 6)], coverage);

        Assert.Equal("Untested", reports[0].Method.FullName);
        Assert.Equal(12.0, reports[0].Crap!.Value, 6);   // 3² + 3
        Assert.Equal(5.0, reports[1].Crap!.Value, 6);    // fully covered: CRAP = CC
    }

    [Fact]
    public void TableShowsDashesWhenThereIsNoCoverage()
    {
        var table = ReportFormatter.Table(MethodReport.Build([Method("A", 2, 1, 2)], null));
        Assert.Contains("A  (Foo.cs:1)", table);
        Assert.Contains("-", table);
    }

    [Fact]
    public void TableShowsRoundedScoreAndCoveragePercent()
    {
        var coverage = new CoverageMap();
        coverage.Record(File1, 1, 1);
        coverage.Record(File1, 2, 0);
        var table = ReportFormatter.Table(MethodReport.Build([Method("A", 2, 1, 2)], coverage));
        Assert.Contains("50%", table);
        Assert.Contains("2.50", table); // 4 * 0.125 + 2
    }

    [Fact]
    public void TableTruncatesSoAPassingScoreNeverReadsAsTheThreshold()
    {
        var table = ReportFormatter.Table([new MethodReport(Method("A", 9, 1, 2), 0.996, 9.996)]);
        Assert.Contains("9.99", table);
        Assert.DoesNotContain("10.0", table);
        Assert.Contains("99%", table);
    }
}
