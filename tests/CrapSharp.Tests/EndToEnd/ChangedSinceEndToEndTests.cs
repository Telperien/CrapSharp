using CrapSharp.Processes;
using Xunit;

namespace CrapSharp.Tests.EndToEnd;

[Trait("Category", "EndToEnd")]
public class ChangedSinceEndToEndTests
{
    [EndToEndFact]
    public void ChangedSinceSeesWhatWasCommittedAfterTheReferenceAndWhatIsStillUncommitted()
    {
        using var fixture = FixtureCopy.Of("basic");
        Assert.Equal(0, fixture.Run().ExitCode);
        File.WriteAllText(Path.Combine(fixture.Root, ".gitignore"), "bin/\nobj/\n.metrics/\n");
        Git(fixture.Root, "init", "-q", "-b", "main");
        Git(fixture.Root, "add", "-A");
        Commit(fixture.Root, "-m", "initial");
        Git(fixture.Root, "tag", "base");
        File.AppendAllText(Path.Combine(fixture.Root, "src", "App", "Decisions.cs"), "\n// committed after the tag\n");
        Commit(fixture.Root, "-am", "edit Decisions");
        File.AppendAllText(Path.Combine(fixture.Root, "src", "App", "Program.cs"), "\n// still uncommitted\n");

        // Both runs write the same .metrics/crap.json: read each result before the next run overwrites it.
        var since = fixture.Run("--use-existing-coverage", "--changed-since", "base", "--threshold", "10.01");
        var sinceNames = since.Names().Order().ToList();
        string? recordedReference;
        using (var json = since.Json())
            recordedReference = json.RootElement.GetProperty("scope").GetProperty("changedSince").GetString();
        var plain = fixture.Run("--use-existing-coverage", "--changed", "--threshold", "10.01");

        Assert.Equal(0, since.ExitCode);
        Assert.Equal(["<top-level statements>", "App.Decisions.Ten(int)", "App.Decisions.Untouched(int)"], sinceNames);
        Assert.Equal("base", recordedReference);
        // --changed only sees the uncommitted file: the commit made after the tag is invisible to it.
        Assert.Equal(["<top-level statements>"], plain.Names());
    }

    [EndToEndFact]
    public void AnUnknownReferenceIsAnErrorThatNamesTheFlag()
    {
        using var fixture = FixtureCopy.Of("basic");
        Git(fixture.Root, "init", "-q", "-b", "main");
        Git(fixture.Root, "add", "-A");
        Commit(fixture.Root, "-m", "initial");

        var run = fixture.Run("--no-coverage", "--changed-since", "no-such-branch");

        Assert.Equal(1, run.ExitCode);
        Assert.Contains("--changed-since 'no-such-branch' needs a reference git knows", run.Error);
    }

    private static void Commit(string directory, params string[] args) =>
        Git(directory, ["-c", "user.name=t", "-c", "user.email=t@example.com", "commit", "-q", .. args]);

    private static void Git(string directory, params string[] args)
    {
        var result = new SystemProcessRunner().Run("git", args, directory);
        Assert.True(result.ExitCode == 0, result.Combined);
    }
}
