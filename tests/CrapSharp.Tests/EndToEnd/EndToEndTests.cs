using Xunit;

namespace CrapSharp.Tests.EndToEnd;

/// <summary>
/// The published tool on real mini-solutions (tests/fixtures). Every expected value was worked out by hand from the
/// fixture sources: CC = 1 + decisions (PLAN.md 4.2), coverage = hit lines / reported lines of the member,
/// CRAP = CC^2 * (1 - coverage)^3 + CC. Where coverlet reported something the hand model did not foresee, the test
/// pins what coverlet reported and the comment says why.
/// </summary>
[Trait("Category", "EndToEnd")]
public class EndToEndTests
{
    private const int Precision = 6;

    private static void AssertEntry(Entry entry, int complexity, double coveragePercent, double crap)
    {
        Assert.Equal(complexity, entry.Complexity);
        Assert.Equal(coveragePercent, entry.Coverage);
        Assert.Equal(crap, entry.CrapRaw, Precision);
    }

    [EndToEndFact]
    public void EveryTargetFrameworkOfAMultiTargetTestProjectCountsTowardsTheCoverage()
    {
        using var fixture = FixtureCopy.Of("multitarget");

        var run = fixture.Run("--threshold", "10");

        // net9.0 runs Abs, net10.0 runs Twice: only the union of both reports shows both at 100 %.
        // CC 2, every line hit: CRAP = 2^2 * 0^3 + 2 = 2. With one report lost, Twice would score 4 + 2 = 6.
        Assert.Equal(0, run.ExitCode);
        AssertEntry(run.EntryOf("Calc.Abs(int)"), 2, 100, 2);
        AssertEntry(run.EntryOf("Calc.Twice(int)"), 2, 100, 2);
        Assert.Equal(2, run.CoverageReports().Count);
        Assert.All(run.CoverageReports(), report => Assert.True(File.Exists(report)));
    }

    [EndToEndFact]
    public void EveryTestProjectContributesItsOwnReport()
    {
        using var fixture = FixtureCopy.Of("twotestprojects");

        var run = fixture.Run("--threshold", "10");

        // Alpha.Tests only runs Alpha, Beta.Tests only runs Beta; each Pick is CC 2 and fully hit: CRAP 2.
        Assert.Equal(0, run.ExitCode);
        AssertEntry(run.EntryOf("Alpha.Pick(bool)"), 2, 100, 2);
        AssertEntry(run.EntryOf("Beta.Pick(bool)"), 2, 100, 2);
        Assert.Equal(2, run.CoverageReports().Count);
    }

    [EndToEndFact]
    public void MembersExcludedFromCodeCoverageAreAbsentFromBothCoverletAndTheTable()
    {
        using var fixture = FixtureCopy.Of("excluded");

        var run = fixture.Run("--threshold", "10");

        // Measured: CC 2, both branches run: CRAP 2. The excluded method, property and type are not scored.
        Assert.Equal(0, run.ExitCode);
        Assert.Equal(["Lib.Rules.Measured(int)"], run.Names());
        AssertEntry(run.EntryOf("Rules.Measured(int)"), 2, 100, 2);
        // Coverlet really omitted them: scoring them would otherwise have shown 0 % covered.
        var report = File.ReadAllText(Assert.Single(run.CoverageReports()));
        Assert.DoesNotContain("ExcludedMethod", report);
        Assert.DoesNotContain("ExcludedProperty", report);
        Assert.DoesNotContain("Never", report);
    }

    [EndToEndFact]
    public void LambdasLocalFunctionsAsyncAndIteratorsAreScoredWithTheirEnclosingMember()
    {
        using var fixture = FixtureCopy.Of("closures");

        var run = fixture.Run();

        // Score: CC = 1 + if (local function) + if (lambda) + foreach + ?: = 5. Lines reported by coverlet: 6, 8-12
        // (local function), 13-18 (delegate and lambda body), 19-23 = 17 lines. The hand model expected 12 hit
        // (the lambda body, lines 14-17, never runs and the line 13 statement does); coverlet also reports line 18
        // (the closing `};` of the statement that creates the delegate) as hit: 13/17 = 76.47 %.
        // CRAP = 5^2 * (4/17)^3 + 5 = 1600/4913 + 5.
        var score = run.EntryOf("Closures.Score(int[], bool)");
        AssertEntry(score, 5, 76.4, 5 + 1600.0 / 4913);
        // LoadAsync: CC 2; the state machine reports 6 lines (26-31), the early return (line 29) is the only miss:
        // 5/6 = 83.3 %, CRAP = 2^2 * (1/6)^3 + 2 = 4/216 + 2.
        AssertEntry(run.EntryOf("Closures.LoadAsync(bool)"), 2, 83.3, 2 + 4.0 / 216);
        // Evens: CC = 1 + for + if = 3, all 7 reported lines hit: CRAP 3.
        AssertEntry(run.EntryOf("Closures.Evens(int)"), 3, 100, 3);
    }

    [EndToEndFact]
    public void TwoMembersOnTheSameLineShareTheirCoverage()
    {
        using var fixture = FixtureCopy.Of("sameline");

        var run = fixture.Run("--threshold", "10");

        // Known limit (PLAN.md 4.4), pinned here: First and Second share line 5 and the tests only call First,
        // yet both read 100 % (CC 2, CRAP 2). Second is in truth untested: the line approximation cannot tell.
        Assert.Equal(0, run.ExitCode);
        AssertEntry(run.EntryOf("Pair.First(int)"), 2, 100, 2);
        AssertEntry(run.EntryOf("Pair.Second(int)"), 2, 100, 2);
    }

    [EndToEndFact]
    public void AFailingTestStillPrintsTheReportButExitsWithThree()
    {
        using var fixture = FixtureCopy.Of("failingtest");

        var run = fixture.Run("--threshold", "10");

        // Sign: CC 3 (two ifs). Coverlet reports lines 6-12 (7 lines); `return 0;` (line 11) is never reached:
        // 6/7 = 85.7 %, CRAP = 3^2 * (1/7)^3 + 3 = 9/343 + 3, under the threshold. The failed test still wins: code 3.
        Assert.Equal(3, run.ExitCode);
        Assert.Contains("Lib.Rules.Sign(int)", run.Output);
        Assert.Contains("incomplete coverage", run.Error);
        AssertEntry(run.EntryOf("Rules.Sign(int)"), 3, 85.7, 3 + 9.0 / 343);
    }

    [EndToEndFact]
    public void ADeterministicBuildIsScoredWhenCoverletResolvesTheSourcePaths()
    {
        using var fixture = FixtureCopy.Of("deterministic");

        var run = fixture.Run("--threshold", "10");

        // The pdb maps the sources to /_/ but coverlet's default maps them back: Sign is CC 2, fully hit, CRAP 2.
        Assert.Equal(0, run.ExitCode);
        AssertEntry(run.EntryOf("Rules.Sign(int)"), 2, 100, 2);
    }

    [EndToEndFact]
    public void ADeterministicReportWithUnresolvedPathsIsAnErrorNotAFalseZeroPercent()
    {
        using var fixture = FixtureCopy.Of("deterministic-paths");

        var run = fixture.Run("--threshold", "10");

        // DeterministicReport=true keeps /_/src/Lib/Rules.cs in the Cobertura file: nothing matches (PLAN.md 4.4).
        Assert.Equal(1, run.ExitCode);
        Assert.Contains("none of the 1 analyzed file(s) appears in the coverage report", run.Error);
        Assert.Contains("/_/", run.Error);
        Assert.Empty(run.Output);
        Assert.False(run.HasJson);
        Assert.Contains("/_/src/Lib/Rules.cs", File.ReadAllText(run.CoverageReportsOnDisk().Single()));
    }

    [EndToEndFact]
    public void AMicrosoftTestingPlatformProjectIsReportedAsUnsupportedNotScoredAtZero()
    {
        using var fixture = FixtureCopy.Of("mtp");

        var run = fixture.Run("--threshold", "10");

        // On the .NET 10 SDK, `dotnet test` refuses an MTP project that did not opt in (VSTest target): no report.
        Assert.Equal(1, run.ExitCode);
        Assert.Contains("Microsoft.Testing.Platform", run.Error);
        Assert.Empty(run.Output);
        Assert.False(run.HasJson);
    }

    [EndToEndFact]
    public void AScoreEqualToTheThresholdFailsAndTheRawScoreAndTopLevelStatementsAreReported()
    {
        using var fixture = FixtureCopy.Of("basic");

        var run = fixture.Run("--threshold", "10");

        // Ten: nine ifs, CC 10, every line hit: CRAP = 10^2 * 0^3 + 10 = 10 exactly, which is not below 10.
        // Untouched: CC 2, never called, 0 %: CRAP = 2^2 * 1^3 + 2 = 6. Program.cs (top-level statements, one ternary):
        // one entry, CC 2, never run, 0 %: CRAP 6.
        Assert.Equal(2, run.ExitCode);
        Assert.Contains("1 method(s) score 10 or more", run.Error);
        AssertEntry(run.EntryOf("Decisions.Ten(int)"), 10, 100, 10);
        AssertEntry(run.EntryOf("Decisions.Untouched(int)"), 2, 0, 6);
        AssertEntry(run.EntryOf("<top-level statements>"), 2, 0, 6);
        Assert.Contains("10.00   10   100%  App.Decisions.Ten(int)", run.Output);

        // The same reports, a threshold just above: every score is below it.
        var reused = fixture.Run("--use-existing-coverage", "--threshold", "10.01");
        Assert.Equal(0, reused.ExitCode);
        Assert.Equal(run.CoverageReports(), reused.CoverageReports());
    }

    [EndToEndFact]
    public void AFilteredRunIsMarkedPartialAndAFilterThatMatchesNothingIsAnError()
    {
        using var fixture = FixtureCopy.Of("basic");
        Assert.Equal(2, fixture.Run("--threshold", "10").ExitCode);

        var filtered = fixture.Run("--use-existing-coverage", "--filter", "Decisions", "--threshold", "10.01");

        Assert.Equal(0, filtered.ExitCode);
        Assert.Equal(["App.Decisions.Ten(int)", "App.Decisions.Untouched(int)"], filtered.Names().Order().ToList());
        using (var json = filtered.Json())
            Assert.False(json.RootElement.GetProperty("scope").GetProperty("complete").GetBoolean());

        var typo = fixture.Run("--use-existing-coverage", "--filter", "Decisons");
        Assert.Equal(1, typo.ExitCode);
        Assert.Contains("matches none of the", typo.Error);

        // Not a git repository: --changed cannot say what changed.
        Assert.Equal(1, fixture.Run("--use-existing-coverage", "--changed").ExitCode);
    }

    [EndToEndFact]
    public void BadInputNeverProducesAVerdict()
    {
        using var fixture = FixtureCopy.Of("basic");
        var broken = Path.Combine(fixture.Root, "broken.xml");
        File.WriteAllText(broken, "<coverage><not-closed>");

        var invalidReport = fixture.Run("--coverage", broken, "--threshold", "10");
        var nanThreshold = fixture.Run("--threshold", "NaN");
        var missingValue = fixture.Run("--threshold", "--changed");

        Assert.Equal(1, invalidReport.ExitCode);
        Assert.Contains("Cannot read coverage report", invalidReport.Error);
        Assert.Equal(1, nanThreshold.ExitCode);
        Assert.Contains("finite number above 0", nanThreshold.Error);
        Assert.Equal(1, missingValue.ExitCode);
        Assert.Contains("--threshold expects a value", missingValue.Error);
        Assert.All(new[] { invalidReport, nanThreshold, missingValue }, run => Assert.Empty(run.Output));
        Assert.False(fixture.Run("--no-coverage").HasJson);
    }

    [EndToEndFact]
    public void ATestProjectWithoutACollectorIsNamedAndWhatOnlyItCoversScoresZero()
    {
        using var fixture = FixtureCopy.Of("onecollectorless");

        var run = fixture.Run("--threshold", "10");

        // Alpha.Tests records Alpha: CC 2, 100 %, CRAP 2. Beta.Tests has no coverlet.collector: no report, so Beta
        // (which only it runs) is CC 2, 0 %, CRAP 6. Missing coverage only raises scores; the warning names the project.
        Assert.Equal(0, run.ExitCode);
        Assert.Contains("warning: Beta.Tests.csproj produced no coverage report", run.Error);
        AssertEntry(run.EntryOf("Alpha.Pick(bool)"), 2, 100, 2);
        AssertEntry(run.EntryOf("Beta.Pick(bool)"), 2, 0, 6);
        Assert.Single(run.CoverageReports());
    }

    [EndToEndFact]
    public void AllTestProjectsWithoutACollectorIsAnError()
    {
        using var fixture = FixtureCopy.Of("nocollector");

        var run = fixture.Run("--threshold", "10");

        Assert.Equal(1, run.ExitCode);
        Assert.Contains("produced no coverage report", run.Error);
        Assert.Contains("coverlet.collector", run.Error);
        Assert.Empty(run.Output);
        Assert.False(run.HasJson);
    }
}
