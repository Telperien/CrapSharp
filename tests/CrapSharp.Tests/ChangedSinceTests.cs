using CrapSharp.Git;
using CrapSharp.Processes;
using Xunit;

namespace CrapSharp.Tests;

/// <summary>--changed-since &lt;ref&gt;: the files changed since a reference, committed or not, for a pull request check.</summary>
public class ChangedSinceTests
{
    private sealed record Outcome(int ExitCode, string Output, string Error);

    private static Outcome Run(Options options, FakeProcessRunner runner)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var exit = new App(runner, output, error).Run(options);
        return new Outcome(exit, output.ToString(), error.ToString());
    }

    private static FakeProcessRunner Git(string root, string diff, string untracked = "") => new((_, args, _) => args switch
    {
        ["rev-parse", ..] => new ProcessResult(0, root + "\n", ""),
        ["merge-base", ..] => new ProcessResult(0, "abc123\n", ""),
        ["-c", ..] => new ProcessResult(0, diff, ""),
        _ => new ProcessResult(0, untracked, ""),
    });

    [Fact]
    public void TheReferenceMustBeGivenAValueAndCannotBeCombinedWithChanged()
    {
        Assert.Equal("origin/main", Options.Parse(["--changed-since", "origin/main"]).ChangedSince);
        Assert.Null(Options.Parse([]).ChangedSince);
        Assert.Equal("--changed-since expects a value", Assert.Throws<OptionsException>(() => Options.Parse(["--changed-since"])).Message);
        Assert.Equal(
            "Choose one of --changed and --changed-since",
            Assert.Throws<OptionsException>(() => Options.Parse(["--changed", "--changed-since", "main"])).Message);
    }

    [Fact]
    public void GitIsAskedForTheCommonAncestorThenForTheChangedAndUntrackedFiles()
    {
        var runner = Git("/repo", "src/A.cs\nREADME.md\nsrc/B.CS\n", "src/New.cs\n");

        var files = new ChangedFiles(runner).FindSince("/repo/sub", "origin/main");

        Assert.Equal(["rev-parse", "--show-toplevel"], runner.Calls[0].Arguments);
        Assert.Equal(["merge-base", "origin/main", "HEAD"], runner.Calls[1].Arguments);
        Assert.Equal(["-c", "core.quotepath=false", "diff", "--name-only", "--diff-filter=ACMR", "abc123"], runner.Calls[2].Arguments);
        Assert.Equal(["ls-files", "--others", "--exclude-standard"], runner.Calls[3].Arguments);
        Assert.All(runner.Calls, call => Assert.Equal("git", call.File));
        Assert.Equal(
            new[] { "src/A.cs", "src/B.CS", "src/New.cs" }.Select(path => Path.GetFullPath(Path.Combine("/repo", path))).Order().ToList(),
            files.Order().ToList());
    }

    [Fact]
    public void AnUnknownReferenceOrAShallowCloneIsExplained()
    {
        var runner = new FakeProcessRunner((_, args, _) => args[0] == "rev-parse"
            ? new ProcessResult(0, "/repo\n", "")
            : new ProcessResult(1, "", "fatal: Not a valid object name nope"));

        var e = Assert.Throws<InvalidOperationException>(() => new ChangedFiles(runner).FindSince("/repo", "nope"));

        Assert.Contains("--changed-since 'nope' needs a reference git knows", e.Message);
        Assert.Contains("fetch-depth", e.Message);
    }

    [Fact]
    public void OnlyTheFilesChangedSinceTheReferenceAreScoredAndTheJsonSaysSo()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();
        ws.Write("src/App/Other.cs", "class Other { void M() { } }");
        var report = ws.Write("cov.xml", TempWorkspace.CoberturaXml(source, (6, 1), (7, 0)));

        var outcome = Run(new Options(ws.Root, [report], ChangedSince: "origin/main"), Git(ws.Root, "src/App/Calc.cs\n"));

        Assert.Equal(0, outcome.ExitCode);
        Assert.Contains("Calc.Sign", outcome.Output);
        Assert.DoesNotContain("Other.M", outcome.Output);
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(ws.Path_(".metrics/crap.json")));
        var scope = doc.RootElement.GetProperty("scope");
        Assert.Equal("origin/main", scope.GetProperty("changedSince").GetString());
        Assert.False(scope.GetProperty("complete").GetBoolean());
    }

    [Fact]
    public void NothingChangedSinceTheReferenceIsASuccessThatRunsNothing()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();
        var git = Git(ws.Root, "");

        var outcome = Run(new Options(ws.Root, [], Threshold: 10, ChangedSince: "main"), git);

        Assert.Equal(0, outcome.ExitCode);
        Assert.Contains("no changed C# source file to score", outcome.Error);
        Assert.DoesNotContain(git.Calls, call => call.File == "dotnet");
    }

    [Fact]
    public void AGitFailureIsReportedAsAnError()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();
        var git = new FakeProcessRunner((_, _, _) => new ProcessResult(128, "", "fatal: not a git repository"));

        var outcome = Run(new Options(ws.Root, [], NoCoverage: true, ChangedSince: "main"), git);

        Assert.Equal(1, outcome.ExitCode);
        Assert.Contains("--changed", outcome.Error);
    }
}
