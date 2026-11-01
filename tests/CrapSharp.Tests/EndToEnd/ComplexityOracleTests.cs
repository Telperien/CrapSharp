using System.Text.RegularExpressions;
using CrapSharp.Complexity;
using CrapSharp.Processes;
using Xunit;

namespace CrapSharp.Tests.EndToEnd;

/// <summary>
/// Cross-checks the complexity against Microsoft's own code-metrics analyzer (rule CA1502, the metric Visual Studio
/// shows), which walks the compiler's operation tree and shares no code with this tool. Setting its threshold to 1
/// makes it report the complexity of every method. Slow (a build per fixture), so it is end-to-end only.
/// </summary>
[Trait("Category", "EndToEnd")]
public partial class ComplexityOracleTests
{
    // CA1502 messages are localized but always quote the method name, then its complexity, then the threshold.
    [GeneratedRegex(@"CA1502: '([^']+)'[^']*'(\d+)'")]
    private static partial Regex Report();

    public static TheoryData<string, string> Fixtures => new()
    {
        { "basic", "src/App/App.csproj" },
        { "closures", "src/Lib/Lib.csproj" },
        { "failingtest", "src/Lib/Lib.csproj" },
        { "sameline", "src/Lib/Lib.csproj" },
        { "multitarget", "src/Lib/Lib.csproj" },
    };

    [EndToEndTheory]
    [MemberData(nameof(Fixtures))]
    public void ComplexityMatchesTheMicrosoftAnalyzerOnEveryMethod(string fixtureName, string project)
    {
        using var fixture = FixtureCopy.Of(fixtureName);
        File.WriteAllText(Path.Combine(fixture.Root, "CodeMetricsConfig.txt"), "CA1502: 1\n");
        File.WriteAllText(Path.Combine(fixture.Root, ".editorconfig"),
            "root = true\n[*.cs]\ndotnet_diagnostic.CA1502.severity = warning\ndotnet_code_quality.CA1502.threshold = 1\n");
        File.WriteAllText(Path.Combine(fixture.Root, "Directory.Build.props"),
            "<Project><PropertyGroup><EnableNETAnalyzers>true</EnableNETAnalyzers><AnalysisLevel>latest-all</AnalysisLevel></PropertyGroup>"
            + "<ItemGroup><AdditionalFiles Include=\"$(MSBuildThisFileDirectory)CodeMetricsConfig.txt\" /></ItemGroup></Project>");
        var build = new SystemProcessRunner().Run(
            "dotnet", ["build", Path.Combine(fixture.Root, project), "--no-incremental", "-p:TreatWarningsAsErrors=false"], fixture.Root);
        var oracle = Report().Matches(build.Combined)
            .Select(m => (Name: m.Groups[1].Value, Complexity: int.Parse(m.Groups[2].Value)))
            .Distinct()
            .ToList();

        var ours = Directory.EnumerateFiles(Path.Combine(fixture.Root, "src"), "*.cs", SearchOption.AllDirectories)
            .SelectMany(file => ComplexityCalculator.Analyze(File.ReadAllText(file), file))
            .Select(method => (Name: ShortName(method.FullName), method.Complexity))
            .Distinct()
            .OrderBy(entry => entry.Name, StringComparer.Ordinal)
            .ToList();

        Assert.True(oracle.Count > 0, build.Combined);
        Assert.Equal(ours, oracle.OrderBy(entry => entry.Name, StringComparer.Ordinal).ToList());
    }

    // "Lib.Decisions.Ten(int)" -> "Ten"; the analyzer calls top-level statements "<Main>$".
    private static string ShortName(string fullName)
    {
        var name = fullName.Split('(')[0].Split('.').Last();
        return name == ComplexityCalculator.TopLevelStatementsName ? "<Main>$" : name;
    }
}
