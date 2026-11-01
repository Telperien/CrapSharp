using System.Text.Json;
using CrapSharp;
using CrapSharp.Complexity;
using CrapSharp.Coverage;
using CrapSharp.Scoring;
using Xunit;

namespace CrapSharp.Tests;

public class JsonAndThresholdTests
{
    private static readonly string File1 = Path.GetFullPath("Foo.cs");

    private static IReadOnlyList<MethodReport> Reports(bool withCoverage)
    {
        var methods = new[] { new MethodComplexity("C.M()", File1, 1, 2, 2) };
        CoverageMap? coverage = null;
        if (withCoverage)
        {
            coverage = new CoverageMap();
            coverage.Record(File1, 1, 1);
            coverage.Record(File1, 2, 0);
        }
        return MethodReport.Build(methods, coverage);
    }

    [Fact]
    public void JsonContainsOneEntryPerMethodWithPercentCoverage()
    {
        using var doc = JsonDocument.Parse(JsonReportWriter.Serialize(Reports(withCoverage: true)));
        var entry = doc.RootElement.GetProperty("entries")[0];
        Assert.Equal("C.M()", entry.GetProperty("name").GetString());
        Assert.Equal(2, entry.GetProperty("complexity").GetInt32());
        Assert.Equal(1, entry.GetProperty("startLine").GetInt32());
        Assert.Equal(50.0, entry.GetProperty("coverage").GetDouble());
        Assert.Equal(2.5, entry.GetProperty("crap").GetDouble());
    }

    [Fact]
    public void JsonUsesNullWithoutCoverage()
    {
        using var doc = JsonDocument.Parse(JsonReportWriter.Serialize(Reports(withCoverage: false)));
        var entry = doc.RootElement.GetProperty("entries")[0];
        Assert.Equal(JsonValueKind.Null, entry.GetProperty("crap").ValueKind);
        Assert.Equal(JsonValueKind.Null, entry.GetProperty("coverage").ValueKind);
    }

    [Fact]
    public void WriteCreatesTheMetricsDirectory()
    {
        var dir = Directory.CreateTempSubdirectory("crapsharp-json").FullName;
        try
        {
            var file = Path.Combine(dir, ".metrics", "crap.json");
            JsonReportWriter.Write(Reports(withCoverage: true), file);
            Assert.True(File.Exists(file));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // The single method scores exactly 2.5: every score must be strictly below the threshold.
    [Theory]
    [InlineData(2.0, true)]
    [InlineData(2.5, true)]
    [InlineData(2.51, false)]
    [InlineData(10.0, false)]
    public void AScoreEqualToTheThresholdFails(double threshold, bool fails) =>
        Assert.Equal(fails, ThresholdCheck.Failing(Reports(withCoverage: true), threshold).Count == 1);

    [Fact]
    public void ReportsWithoutScoreNeverFailTheThreshold() =>
        Assert.Empty(ThresholdCheck.Failing(Reports(withCoverage: false), 0));

    [Fact]
    public void JsonValuesAreTruncatedNotRounded()
    {
        var method = new MethodComplexity("C.M()", File1, 1, 2, 9);
        var json = JsonReportWriter.Serialize([new MethodReport(method, 0.99999, 9.999)]);
        using var doc = JsonDocument.Parse(json);
        var entry = doc.RootElement.GetProperty("entries")[0];
        Assert.Equal(99.9, entry.GetProperty("coverage").GetDouble());
        Assert.Equal(9.99, entry.GetProperty("crap").GetDouble());
    }

    [Fact]
    public void JsonKeepsTheRawScoreTheVerdictIsBasedOn()
    {
        var method = new MethodComplexity("C.M()", File1, 1, 2, 9);
        using var doc = JsonDocument.Parse(JsonReportWriter.Serialize([new MethodReport(method, 0.99999, 9.999)]));
        var entry = doc.RootElement.GetProperty("entries")[0];
        Assert.Equal(9.999, entry.GetProperty("crapRaw").GetDouble());
        Assert.Equal(9.99, entry.GetProperty("crap").GetDouble());
    }

    [Fact]
    public void JsonStatesTheScopeAndWhereTheCoverageCameFrom()
    {
        var context = new ReportContext(Changed: false, Filter: "Orders", CoverageMode.Explicit, ["a.xml", "b.xml"], FilesAnalyzed: 3);
        using var doc = JsonDocument.Parse(JsonReportWriter.Serialize(Reports(withCoverage: true), context));
        var scope = doc.RootElement.GetProperty("scope");
        Assert.False(scope.GetProperty("complete").GetBoolean());
        Assert.Equal("Orders", scope.GetProperty("filter").GetString());
        Assert.Equal(3, scope.GetProperty("files").GetInt32());
        var coverage = doc.RootElement.GetProperty("coverage");
        Assert.Equal("Explicit", coverage.GetProperty("source").GetString());
        Assert.Equal(["a.xml", "b.xml"], coverage.GetProperty("reports").EnumerateArray().Select(r => r.GetString()));
    }

    [Theory]
    [InlineData(false, null, true)]
    [InlineData(true, null, false)]
    [InlineData(false, "x", false)]
    public void OnlyAnUnfilteredRunIsACompleteReport(bool changed, string? filter, bool complete) =>
        Assert.Equal(complete, new ReportContext(changed, filter, CoverageMode.Run, [], 1).Complete);

    [Fact]
    public void ParseReadsTheThreshold()
    {
        Assert.Equal(10.5, Options.Parse(["--threshold", "10.5", "src"]).Threshold);
        Assert.Null(Options.Parse(["src"]).Threshold);
    }

    [Fact]
    public void ParseRejectsANonNumericThreshold()
    {
        var e = Assert.Throws<OptionsException>(() => Options.Parse(["--threshold", "abc"]));
        Assert.Contains("--threshold expects a finite number above 0", e.Message);
    }
}
