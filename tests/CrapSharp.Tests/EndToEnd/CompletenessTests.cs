using Xunit;
using static CrapSharp.Tests.EndToEnd.EndToEndTests;

namespace CrapSharp.Tests.EndToEnd;

/// <summary>Cases where the coverage may not describe the sources: unused projects, stale reports, empty or narrowed selections.</summary>
[Trait("Category", "EndToEnd")]
public class CompletenessTests
{
    [EndToEndFact]
    public void AProductionProjectNoTestUsesIsWarnedAboutAndScoredZero()
    {
        using var fixture = FixtureCopy.Of("unusedproject");

        var run = fixture.Run("--threshold", "10");

        // Alpha.Pick: CC 2, covered. Orphan's assembly is never loaded by a test, so coverlet does not mention its file:
        // Calculator.Positive is CC 2, 0 %, CRAP = 2^2 * 1^3 + 2 = 6, and the warning names the project (docs/conventions.md, Coverage).
        Assert.Equal(0, run.ExitCode);
        Assert.Matches(@"warning: coverage report does not mention 1 analyzed file\(s\) in project src.Orphan.Orphan\.csproj", run.Error);
        AssertEntry(run.EntryOf("Alpha.Pick(bool)"), 2, 100, 2);
        AssertEntry(run.EntryOf("Calculator.Positive(int)"), 2, 0, 6);
    }

    [EndToEndFact]
    public void ATreeWithOnlyConstantsIsAnErrorBeforeAnyTestRuns()
    {
        using var fixture = FixtureCopy.Of("nomembers");

        var run = fixture.Run("--threshold", "10");

        Assert.Equal(1, run.ExitCode);
        Assert.Contains("no measurable member found in the 1 analyzed file(s)", run.Error);
        Assert.DoesNotContain("Running tests", run.Error);
        Assert.False(run.HasJson);
    }

    [EndToEndFact]
    public void ASourceEditedAfterTheReusedReportsWereWrittenIsFlaggedAsStale()
    {
        using var fixture = FixtureCopy.Of("basic");
        Assert.Equal(0, fixture.Run().ExitCode);

        var fresh = fixture.Run("--use-existing-coverage");
        File.AppendAllText(Path.Combine(fixture.Root, "src", "App", "Decisions.cs"), "\n// edited after the tests ran\n");
        var stale = fixture.Run("--use-existing-coverage");

        Assert.DoesNotContain("may be stale", fresh.Error);
        Assert.Contains("1 source file(s) changed after the coverage report was written", stale.Error);
        Assert.Equal(0, stale.ExitCode);
    }

    [EndToEndFact]
    public void ChangedFollowsTheRealGitStatusIncludingNothingChanged()
    {
        using var fixture = FixtureCopy.Of("basic");
        Assert.Equal(0, fixture.Run().ExitCode);
        File.WriteAllText(Path.Combine(fixture.Root, ".gitignore"), "bin/\nobj/\n.metrics/\n");
        Git(fixture.Root, "init", "-q");
        Git(fixture.Root, "add", "-A");
        Git(fixture.Root, "-c", "user.name=t", "-c", "user.email=t@example.com", "commit", "-q", "-m", "initial");

        var unchanged = fixture.Run("--use-existing-coverage", "--changed", "--threshold", "10");
        File.AppendAllText(Path.Combine(fixture.Root, "src", "App", "Decisions.cs"), "\n// touched\n");
        var changed = fixture.Run("--use-existing-coverage", "--changed", "--threshold", "10.01");

        Assert.Equal(0, unchanged.ExitCode);
        Assert.Contains("no changed C# source file to score", unchanged.Error);
        Assert.Equal(0, changed.ExitCode);
        Assert.Equal(["App.Decisions.Ten(int)", "App.Decisions.Untouched(int)"], changed.Names().Order().ToList());
        using var json = changed.Json();
        Assert.True(json.RootElement.GetProperty("scope").GetProperty("changed").GetBoolean());
        Assert.False(json.RootElement.GetProperty("scope").GetProperty("complete").GetBoolean());
    }

    private static void Git(string directory, params string[] args)
    {
        var result = new CrapSharp.Processes.SystemProcessRunner().Run("git", args, directory);
        Assert.True(result.ExitCode == 0, result.Combined);
    }
}
