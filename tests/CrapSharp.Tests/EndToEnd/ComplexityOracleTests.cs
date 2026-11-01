using System.Text.RegularExpressions;
using CrapSharp.Complexity;
using CrapSharp.Processes;
using Xunit;

namespace CrapSharp.Tests.EndToEnd;

/// <summary>
/// Cross-checks the complexity against Microsoft's code-metrics analyzer (rule CA1502), which walks the compiler's
/// operation tree and shares no code with this tool. Modern Visual Studio metrics use related source analysis, but
/// these fixture values have not been checked in its UI. A threshold of 1 reports methods above 1; the absence of a
/// diagnostic is interpreted as complexity 1 below. Slow (a build per fixture), so it is end-to-end only.
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
        { "constructs", "src/Lib/Lib.csproj" },
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
            .DistinctBy(m => m.Groups[1].Value)
            .ToDictionary(m => m.Groups[1].Value, m => int.Parse(m.Groups[2].Value), StringComparer.Ordinal);

        var ours = Directory.EnumerateFiles(Path.Combine(fixture.Root, "src"), "*.cs", SearchOption.AllDirectories)
            .SelectMany(file => ComplexityCalculator.Analyze(File.ReadAllText(file), file))
            .DistinctBy(method => ShortName(method.FullName))
            .ToDictionary(method => ShortName(method.FullName), method => method.Complexity, StringComparer.Ordinal);

        Assert.True(oracle.Count > 0, build.Combined);
        // The analyzer reports nothing for a complexity of 1.
        var differences = ours
            .Select(entry => (entry.Key, Ours: entry.Value, Oracle: oracle.GetValueOrDefault(entry.Key, 1)))
            .Where(entry => entry.Ours != entry.Oracle)
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(KnownDifferences(fixtureName), differences);
    }

    // Constructs where docs/conventions.md (Complexity) and CA1502 disagree, pinned so that a change on either side is noticed:
    // CA1502 counts the `default` label of a switch statement (6 for 4 labels + default; 4.2 says 5), and counts nothing
    // for `??=`, catch clauses, switch expression arms and `or` patterns (4.2 counts each).
    private static List<(string Key, int Ours, int Oracle)> KnownDifferences(string fixtureName) => fixtureName == "constructs"
        ?
        [
            ("Catches", 3, 1),
            ("Coalesce", 4, 3),
            ("OrPattern", 2, 1),
            ("SwitchExpression", 3, 1),
            ("SwitchStatement", 5, 6),
        ]
        : [];

    // "Lib.Decisions.Ten(int)" -> "Ten"; the analyzer calls top-level statements "<Main>$".
    private static string ShortName(string fullName)
    {
        var name = fullName.Split('(')[0].Split('.').Last();
        return name == ComplexityCalculator.TopLevelStatementsName ? "<Main>$" : name;
    }
}
