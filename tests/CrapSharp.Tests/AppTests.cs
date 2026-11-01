using CrapSharp.Processes;
using Xunit;

namespace CrapSharp.Tests;

public class AppTests
{
    private sealed record Outcome(int ExitCode, string Output, string Error, FakeProcessRunner Runner);

    private static Outcome Run(Options options, FakeProcessRunner runner)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var exit = new App(runner, output, error).Run(options);
        return new Outcome(exit, output.ToString(), error.ToString(), runner);
    }

    // The fake `dotnet test` reports line 6 hit and line 7 missed: Sign() is 50 % covered, CC 2, CRAP 2.5.
    private static FakeProcessRunner CoveringRunner(string source, int exitCode = 0) => new((_, args, _) =>
    {
        var index = args.ToList().IndexOf("--results-directory");
        var report = Path.Combine(args[index + 1], "g", "coverage.cobertura.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(report)!);
        File.WriteAllText(report, TempWorkspace.CoberturaXml(source, (6, 1), (7, 0)));
        return new ProcessResult(exitCode, "", "");
    });

    private static Outcome RunArgs(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var runner = new FakeProcessRunner();
        var exit = new App(runner, output, error).Run(args);
        return new Outcome(exit, output.ToString(), error.ToString(), runner);
    }

    [Fact]
    public void ARunPrintsTheTableAndWritesTheJson()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();

        var outcome = Run(new Options(ws.Root, []), CoveringRunner(source));

        Assert.Equal(0, outcome.ExitCode);
        Assert.Contains("App.Calc.Sign(int)", outcome.Output);
        Assert.Contains("50%", outcome.Output);
        Assert.DoesNotContain("Generated", outcome.Output);
        Assert.True(File.Exists(ws.Path_(".metrics/crap.json")));
    }

    [Fact]
    public void NoCoverageShowsComplexityOnlyAndLaunchesNothing()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();

        var outcome = Run(new Options(ws.Root, [], NoCoverage: true), new FakeProcessRunner());

        Assert.Equal(0, outcome.ExitCode);
        Assert.Contains("App.Calc.Sign(int)", outcome.Output);
        Assert.Empty(outcome.Runner.Calls);
        Assert.False(File.Exists(ws.Path_(".metrics/crap.json")));
    }

    [Theory]
    [InlineData(2.0, 2)]
    [InlineData(2.5, 2)]
    [InlineData(2.6, 0)]
    public void ThresholdGivesExitCodeTwoUnlessEveryScoreIsBelowIt(double threshold, int expected)
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();

        var outcome = Run(new Options(ws.Root, [], threshold), CoveringRunner(source));

        Assert.Equal(expected, outcome.ExitCode);
        Assert.Equal(expected == 2, outcome.Error.Contains("threshold not met"));
    }

    [Fact]
    public void FailingTestsStillPrintTheReportButGiveExitCodeThree()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();

        var outcome = Run(new Options(ws.Root, []), CoveringRunner(source, exitCode: 1));

        Assert.Equal(3, outcome.ExitCode);
        Assert.Contains("App.Calc.Sign(int)", outcome.Output);
        Assert.Contains("incomplete coverage", outcome.Error);
    }

    [Fact]
    public void FailingTestsWinOverAThresholdNotMet()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();

        var outcome = Run(new Options(ws.Root, [], Threshold: 1), CoveringRunner(source, exitCode: 1));

        Assert.Equal(3, outcome.ExitCode);
        Assert.Contains("threshold not met", outcome.Error);
    }

    [Fact]
    public void ACoverageReportThatMentionsNoAnalyzedFileIsFlagged()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();
        var report = ws.Write("cov.xml", TempWorkspace.CoberturaXml("/_/src/App/Calc.cs", (6, 1)));

        var outcome = Run(new Options(ws.Root, [report]), new FakeProcessRunner());

        Assert.Equal(1, outcome.ExitCode);
        Assert.Contains("none of the 1 analyzed file(s) appears in the coverage report", outcome.Error);
        Assert.Empty(outcome.Output);
        Assert.False(File.Exists(ws.Path_(".metrics/crap.json")));
    }

    [Fact]
    public void AnUncoveredProductionProjectIsWarnedAndItsMethodsScoreZeroCoverage()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();
        ws.Write("src/Unused/Unused.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        ws.Write("src/Unused/Other.cs", "namespace Unused; class Other { int Value() => 1; }");
        var report = ws.Write("cov.xml", TempWorkspace.CoberturaXml(source, (6, 1), (7, 0)));

        var outcome = Run(new Options(ws.Root, [report], Threshold: 10), new FakeProcessRunner());

        Assert.Equal(0, outcome.ExitCode);
        Assert.Contains("Unused.Other.Value()", outcome.Output);
        Assert.Contains("0%", outcome.Output);
        Assert.Contains("Unused.csproj", outcome.Error);
        Assert.Contains("warning:", outcome.Error);
    }

    [Fact]
    public void AFileWithNoMeasurableMemberNeverCountsAsMissingFromTheCoverage()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();
        ws.Write("src/App/Codes.cs", "namespace App; static class Codes { public const int Ok = 0; }");
        var report = ws.Write("cov.xml", TempWorkspace.CoberturaXml(source, (6, 1), (7, 0)));

        var outcome = Run(new Options(ws.Root, [report]), new FakeProcessRunner());

        Assert.Equal(0, outcome.ExitCode);
        Assert.DoesNotContain("warning", outcome.Error);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void ASourceEditedAfterTheReusedReportWasWrittenIsWarnedAboutAsStale(bool sourceIsNewer, bool warned)
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();
        var report = ws.Write("tests/App.Tests/TestResults/g/coverage.cobertura.xml", TempWorkspace.CoberturaXml(source, (6, 1), (7, 0)));
        var now = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(report, sourceIsNewer ? now.AddMinutes(-10) : now);
        File.SetLastWriteTimeUtc(source, sourceIsNewer ? now : now.AddMinutes(-10));

        var outcome = Run(new Options(ws.Root, [], UseExistingCoverage: true), new FakeProcessRunner());

        Assert.Equal(0, outcome.ExitCode);
        Assert.Equal(warned, outcome.Error.Contains("changed after the coverage report was written"));
    }

    [Fact]
    public void ATreeWithNoMeasurableMemberIsAnErrorNotAnEmptyPass()
    {
        using var ws = new TempWorkspace();
        ws.Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        ws.Write("src/App/Codes.cs", "namespace App; static class Codes { public const int Ok = 0; }");

        var outcome = Run(new Options(ws.Root, [], NoCoverage: true), new FakeProcessRunner());

        Assert.Equal(1, outcome.ExitCode);
        Assert.Contains("no measurable member found in the 1 analyzed file(s)", outcome.Error);
        Assert.Empty(outcome.Output);
    }

    [Fact]
    public void ASelectionWithNoMeasurableMemberIsAcceptedWithANote()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();
        ws.Write("src/App/Codes.cs", "namespace App; static class Codes { public const int Ok = 0; }");

        var outcome = Run(new Options(ws.Root, [], NoCoverage: true, Filter: "Codes"), new FakeProcessRunner());

        Assert.Equal(0, outcome.ExitCode);
        Assert.Contains("no measurable member in the selected file(s)", outcome.Error);
    }

    [Fact]
    public void ACoverageReportThatMentionsTheSourcesIsNotFlagged()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();

        var outcome = Run(new Options(ws.Root, []), CoveringRunner(source));

        Assert.DoesNotContain("none of the", outcome.Error);
    }

    [Fact]
    public void HelpPrintsTheUsageAndRunsNothing()
    {
        var outcome = RunArgs("--help");
        Assert.Equal(0, outcome.ExitCode);
        Assert.Contains("Usage: crapsharp", outcome.Output);
        Assert.Empty(outcome.Runner.Calls);
    }

    [Fact]
    public void ABadCommandLineIsAnErrorThatPointsToTheHelp()
    {
        var outcome = RunArgs("--verbose");
        Assert.Equal(1, outcome.ExitCode);
        Assert.Contains("Unknown option: --verbose", outcome.Error);
        Assert.Contains("--help", outcome.Error);
        Assert.Empty(outcome.Runner.Calls);
    }

    [Fact]
    public void ThresholdWithNoCoverageIsRejectedBeforeAnythingRuns()
    {
        var outcome = Run(new Options(Path.GetTempPath(), [], 10, NoCoverage: true), new FakeProcessRunner());
        Assert.Equal(1, outcome.ExitCode);
        Assert.Contains("--threshold needs coverage", outcome.Error);
        Assert.Empty(outcome.Runner.Calls);
    }

    [Fact]
    public void AMissingPathIsAnError()
    {
        var outcome = Run(new Options("does-not-exist-anywhere", []), new FakeProcessRunner());
        Assert.Equal(1, outcome.ExitCode);
        Assert.Contains("Path not found", outcome.Error);
    }

    [Fact]
    public void FailedCoverageStopsTheRunWithExitCodeOne()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();

        var outcome = Run(new Options(ws.Root, []), new FakeProcessRunner());

        Assert.Equal(1, outcome.ExitCode);
        Assert.Empty(outcome.Output);
    }

    [Fact]
    public void FilterKeepsOnlyMatchingFilesWhateverTheSeparator()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();
        ws.Write("src/App/Other.cs", "class Other { void M() { } }");

        var outcome = Run(new Options(ws.Root, [], NoCoverage: true, Filter: "App\\Calc"), new FakeProcessRunner());

        Assert.Contains("Calc.Sign", outcome.Output);
        Assert.DoesNotContain("Other.M", outcome.Output);
    }

    [Fact]
    public void ChangedKeepsOnlyFilesGitReports()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();
        ws.Write("src/App/Other.cs", "class Other { void M() { } }");
        var git = new FakeProcessRunner((_, args, _) => args[0] == "rev-parse"
            ? new ProcessResult(0, ws.Root + "\n", "")
            : new ProcessResult(0, " M src/App/Other.cs\n", ""));

        var outcome = Run(new Options(ws.Root, [], NoCoverage: true, Changed: true), git);

        Assert.Contains("Other.M", outcome.Output);
        Assert.DoesNotContain("Calc.Sign", outcome.Output);
    }

    [Fact]
    public void AFilteredRunIsMarkedPartialInTheJsonWithTheReportsItUsed()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();
        var report = ws.Write("cov.xml", TempWorkspace.CoberturaXml(source, (6, 1), (7, 0)));

        var outcome = Run(new Options(ws.Root, [report], Filter: "Calc"), new FakeProcessRunner());

        Assert.Equal(0, outcome.ExitCode);
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(ws.Path_(".metrics/crap.json")));
        Assert.False(doc.RootElement.GetProperty("scope").GetProperty("complete").GetBoolean());
        Assert.Equal("Calc", doc.RootElement.GetProperty("scope").GetProperty("filter").GetString());
        Assert.Equal([report], doc.RootElement.GetProperty("coverage").GetProperty("reports").EnumerateArray().Select(r => r.GetString()));
    }

    [Fact]
    public void AFilterThatMatchesNothingIsAnErrorAndRunsNothing()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();

        var outcome = Run(new Options(ws.Root, [], Threshold: 10, Filter: "Typo"), new FakeProcessRunner());

        Assert.Equal(1, outcome.ExitCode);
        Assert.Contains("--filter 'Typo' matches none of the 1 source file(s)", outcome.Error);
        Assert.Empty(outcome.Runner.Calls);
        Assert.False(File.Exists(ws.Path_(".metrics/crap.json")));
    }

    [Fact]
    public void NothingChangedIsASuccessThatRunsNothingAndKeepsThePreviousJson()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();
        var previous = ws.Write(".metrics/crap.json", "{}");
        var git = new FakeProcessRunner((_, args, _) => args[0] == "rev-parse"
            ? new ProcessResult(0, ws.Root + "\n", "")
            : new ProcessResult(0, "", ""));

        var outcome = Run(new Options(ws.Root, [], Threshold: 10, Changed: true), git);

        Assert.Equal(0, outcome.ExitCode);
        Assert.Contains("no changed C# source file", outcome.Error);
        Assert.DoesNotContain(git.Calls, call => call.File == "dotnet");
        Assert.Equal("{}", File.ReadAllText(previous));
    }

    [Fact]
    public void NoProductionSourceAtAllIsAnError()
    {
        using var ws = new TempWorkspace();
        ws.Write("tests/T/T.csproj", "<Project><ItemGroup><PackageReference Include=\"Microsoft.NET.Test.Sdk\" /></ItemGroup></Project>");
        ws.Write("tests/T/UnitTests.cs", "class UnitTests { void M() { } }");

        var outcome = Run(new Options(ws.Root, [], Threshold: 10), new FakeProcessRunner());

        Assert.Equal(1, outcome.ExitCode);
        Assert.Contains("no production C# source file found", outcome.Error);
        Assert.Empty(outcome.Runner.Calls);
    }

    [Fact]
    public void ChangedOutsideAGitRepositoryIsAnError()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();
        var git = new FakeProcessRunner((_, _, _) => new ProcessResult(128, "", "fatal: not a git repository"));

        var outcome = Run(new Options(ws.Root, [], NoCoverage: true, Changed: true), git);

        Assert.Equal(1, outcome.ExitCode);
        Assert.Contains("--changed needs a git repository", outcome.Error);
    }

    [Fact]
    public void StalenessIsJudgedAgainstTheNewestReportOfSeveral()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();
        var oldReport = ws.Write("tests/App.Tests/TestResults/g/coverage.cobertura.xml", TempWorkspace.CoberturaXml(source, (6, 1), (7, 0)));
        var newReport = ws.Write("tests/App.Tests/TestResults/h/coverage.cobertura.xml", TempWorkspace.CoberturaXml(source, (6, 1), (7, 0)));
        var now = DateTime.UtcNow;
        File.SetLastWriteTimeUtc(oldReport, now.AddMinutes(-9));
        File.SetLastWriteTimeUtc(source, now.AddMinutes(-5));
        File.SetLastWriteTimeUtc(newReport, now);

        var outcome = Run(new Options(ws.Root, [], UseExistingCoverage: true), new FakeProcessRunner());

        Assert.DoesNotContain("may be stale", outcome.Error);
    }

    [Fact]
    public void ReportsProducedByThisRunAreNeverCalledStale()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();
        File.SetLastWriteTimeUtc(source, DateTime.UtcNow.AddHours(1));

        var outcome = Run(new Options(ws.Root, []), CoveringRunner(source));

        Assert.DoesNotContain("may be stale", outcome.Error);
    }

    [Fact]
    public void TheUnmatchedReportErrorExplainsWhereToLook()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();
        var report = ws.Write("cov.xml", TempWorkspace.CoberturaXml("/_/src/App/Calc.cs", (6, 1)));

        var outcome = Run(new Options(ws.Root, [report]), new FakeProcessRunner());

        Assert.Contains("Check that the report paths match the sources (deterministic builds can write /_/ paths).", outcome.Error);
    }

    [Fact]
    public void ASourceWrittenAtTheSameInstantAsTheReportIsNotStale()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();
        var report = ws.Write("tests/App.Tests/TestResults/g/coverage.cobertura.xml", TempWorkspace.CoberturaXml(source, (6, 1), (7, 0)));
        var instant = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(report, instant);
        File.SetLastWriteTimeUtc(source, instant);

        var outcome = Run(new Options(ws.Root, [], UseExistingCoverage: true), new FakeProcessRunner());

        Assert.DoesNotContain("may be stale", outcome.Error);
    }

    [Fact]
    public void ExcludeDropsTheFilesWhosePathContainsAnyFragment()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();
        ws.Write("src/App/Client/View.cs", "class View { void Draw() { } }");
        ws.Write("src/App/Tools/Probe.cs", "class Probe { void Run() { } }");

        var outcome = Run(new Options(ws.Root, [], NoCoverage: true, exclude: ["client", "Tools\\"]), new FakeProcessRunner());

        Assert.Equal(0, outcome.ExitCode);
        Assert.Contains("Calc.Sign", outcome.Output);
        Assert.DoesNotContain("View.Draw", outcome.Output);
        Assert.DoesNotContain("Probe.Run", outcome.Output);
    }

    [Fact]
    public void ExcludingEverythingIsAnErrorLikeAFilterThatMatchesNothing()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();

        var outcome = Run(new Options(ws.Root, [], NoCoverage: true, exclude: ["Calc"]), new FakeProcessRunner());

        Assert.Equal(1, outcome.ExitCode);
        Assert.Contains("--exclude leaves none of the 1 source file(s)", outcome.Error);
        Assert.Empty(outcome.Runner.Calls);
    }

    [Fact]
    public void ExcludeCombinesWithFilterAndMakesTheJsonScopePartial()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();
        ws.Write("src/App/Client/View.cs", "class View { void Draw() { } }");
        var report = ws.Write("cov.xml", TempWorkspace.CoberturaXml(source, (6, 1), (7, 0)));

        var outcome = Run(new Options(ws.Root, [report], Filter: "App", exclude: ["Client"]), new FakeProcessRunner());

        Assert.Equal(0, outcome.ExitCode);
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(ws.Path_(".metrics/crap.json")));
        var scope = doc.RootElement.GetProperty("scope");
        Assert.False(scope.GetProperty("complete").GetBoolean());
        Assert.Equal(["Client"], scope.GetProperty("exclude").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public void TheTableLocatesEachMethodByItsPathRelativeToTheAnalyzedFolder()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();
        ws.Write("src/Other/Other.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        ws.Write("src/Other/Calc.cs", "class Calc { void Twin() { } }");

        var outcome = Run(new Options(ws.Root, [], NoCoverage: true), new FakeProcessRunner());

        Assert.Contains("(src/App/Calc.cs:4)", outcome.Output);
        Assert.Contains("(src/Other/Calc.cs:1)", outcome.Output);
    }
}
