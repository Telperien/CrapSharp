using CrapSharp.Git;
using CrapSharp.Processes;
using CrapSharp.Projects;
using Xunit;

namespace CrapSharp.Tests;

public class DiscoveryAndGitTests
{
    [Theory]
    [InlineData("<PackageReference Include=\"Microsoft.NET.Test.Sdk\" Version=\"17\" />", true)]
    [InlineData("<IsTestProject>true</IsTestProject>", true)]
    [InlineData("<PackageReference Include=\"xunit.v3\" />", true)]
    [InlineData("<PackageReference Include=\"NUnit\" />", true)]
    [InlineData("<PackageReference Include=\"MSTest.TestFramework\" />", true)]
    [InlineData("<PackageReference Include=\"Microsoft.CodeAnalysis.CSharp\" />", false)]
    public void TestProjectsAreRecognizedByTheirPackages(string projectText, bool expected) =>
        Assert.Equal(expected, ProjectDiscovery.IsTestProject(projectText));

    [Fact]
    public void SolutionFolderGivesSourcesWithoutTestsBuildOutputOrGeneratedCode()
    {
        using var ws = new TempWorkspace();
        var (source, testProject) = ws.CreateSolution();

        var workspace = ProjectDiscovery.Discover(ws.Root);

        Assert.Equal(ws.Root, workspace.Root);
        Assert.Equal([source], workspace.SourceFiles);
        Assert.Equal([testProject], workspace.TestProjects);
    }

    [Fact]
    public void ASlnOrCsprojPathUsesItsFolderAsRoot()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();
        var sln = ws.Write("All.sln", "");

        Assert.Equal(ws.Root, ProjectDiscovery.Discover(sln).Root);
    }

    [Fact]
    public void WithoutAnyProjectAllCSharpFilesAreSources()
    {
        using var ws = new TempWorkspace();
        var file = ws.Write("A.cs", "class A { }");

        var workspace = ProjectDiscovery.Discover(ws.Root);

        Assert.Equal([file], workspace.SourceFiles);
        Assert.Empty(workspace.TestProjects);
    }

    [Fact]
    public void ALoneCSharpFileIsAnalyzedAsIs()
    {
        using var ws = new TempWorkspace();
        var file = ws.Write("sub/A.cs", "class A { }");
        ws.Write("sub/B.cs", "class B { }");

        var workspace = ProjectDiscovery.Discover(file);

        Assert.Equal([file], workspace.SourceFiles);
        Assert.Equal(Path.GetDirectoryName(file), workspace.Root);
    }

    [Fact]
    public void PorcelainStatusGivesChangedCSharpFilesWithRenamesAndUntracked()
    {
        var root = Path.GetFullPath("repo");
        var status = " M src/A.cs\r\n?? src/B.cs\nR  old.cs -> src/New.cs\n M README.md\n D src/Gone.cs\n";

        var changed = ChangedFiles.Parse(status, root);

        Assert.Equal(4, changed.Count);
        Assert.Contains(Path.GetFullPath(Path.Combine(root, "src/A.cs")), changed);
        Assert.Contains(Path.GetFullPath(Path.Combine(root, "src/New.cs")), changed);
        Assert.DoesNotContain(Path.GetFullPath(Path.Combine(root, "README.md")), changed);
    }

    [Fact]
    public void FindAsksGitForTheRepositoryRootThenTheStatus()
    {
        var root = Path.GetFullPath("repo");
        var runner = new FakeProcessRunner((_, args, _) => args[0] == "rev-parse"
            ? new ProcessResult(0, root + "\n", "")
            : new ProcessResult(0, " M A.cs\n", "warning: noise\n"));

        var changed = new ChangedFiles(runner).Find("anywhere");

        Assert.Equal([Path.Combine(root, "A.cs")], changed);
        Assert.Equal(["git", "git"], runner.Calls.Select(c => c.File));
    }

    [Fact]
    public void FindFailsClearlyOutsideAGitRepository()
    {
        var runner = new FakeProcessRunner((_, _, _) => new ProcessResult(128, "", "fatal: not a git repository"));
        var error = Assert.Throws<InvalidOperationException>(() => new ChangedFiles(runner).Find("x"));
        Assert.Contains("not a git repository", error.Message);
    }

    [Fact]
    public void TheSystemRunnerRunsRealProcessesAndReportsMissingOnes()
    {
        var runner = new SystemProcessRunner();

        var version = runner.Run("dotnet", ["--version"], Path.GetTempPath());
        var missing = runner.Run("crapsharp-no-such-program", [], Path.GetTempPath());

        Assert.Equal(0, version.ExitCode);
        Assert.NotEmpty(version.Output.Trim());
        Assert.Equal(SystemProcessRunner.NotFound, missing.ExitCode);
    }
}
