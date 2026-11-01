using System.Xml.Linq;
using CrapSharp.Complexity;
using CrapSharp.Coverage;
using CrapSharp.Git;
using CrapSharp.Processes;
using CrapSharp.Projects;
using CrapSharp.Scoring;
using Xunit;

namespace CrapSharp.Tests;

/// <summary>Details of the output and of the ordering that users and scripts rely on, found by mutation testing.</summary>
public class OutputAndOrderTests
{
    private static readonly string File1 = Path.GetFullPath("Foo.cs");

    private static MethodComplexity Method(string name, int cc = 2) => new(name, File1, 1, 2, cc);

    private static string Touch(TempWorkspace ws, string relative) => ws.Write(relative, "class X { }");

    [Fact]
    public void TheTableHasAHeaderAndAlignedColumns()
    {
        var table = ReportFormatter.Table([new MethodReport(Method("A"), 0.5, 2.5)]).ReplaceLineEndings("\n");

        Assert.Equal("    CRAP   CC    Cov  Method\n    2.50    2    50%  A  (Foo.cs:1)\n", table);
    }

    [Fact]
    public void WithoutCoverageTheColumnsShowADash()
    {
        var table = ReportFormatter.Table([new MethodReport(Method("A"), null, null)]).ReplaceLineEndings("\n");

        Assert.Equal("    CRAP   CC    Cov  Method\n       -    2      -  A  (Foo.cs:1)\n", table);
    }

    [Fact]
    public void MethodsWithTheSameScoreAreSortedByName()
    {
        var reports = MethodReport.Build([Method("B"), Method("A")], null);

        Assert.Equal(["A", "B"], reports.Select(r => r.Method.FullName));
    }

    [Fact]
    public void TheJsonIsIndentedForHumans()
    {
        var json = JsonReportWriter.Serialize([new MethodReport(Method("A"), 0.5, 2.5)]).ReplaceLineEndings("\n");

        Assert.Contains("\n  \"entries\": [\n", json);
    }

    [Fact]
    public void FilesAreFoundInOrdinalOrderAndBuildOrToolFoldersAreSkipped()
    {
        using var ws = new TempWorkspace();
        var b = Touch(ws, "b.cs");
        var a = Touch(ws, "a.cs");
        Touch(ws, ".git/hooks/x.cs");
        Touch(ws, "node_modules/pkg/y.cs");
        Touch(ws, "bin/z.cs");
        Touch(ws, "obj/w.cs");

        Assert.Equal([a, b], SourceDiscovery.FindCSharpFiles(ws.Root));
    }

    [Fact]
    public void ADiscoveryStartedFromAProjectFileAnalyzesItsFolderNotTheProjectFile()
    {
        using var ws = new TempWorkspace();
        var project = ws.Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        var source = Touch(ws, "src/App/A.cs");

        var workspace = ProjectDiscovery.Discover(project);

        Assert.Equal(Path.GetDirectoryName(project), workspace.Root);
        Assert.Equal([source], workspace.SourceFiles);
    }

    [Fact]
    public void ProjectsAndSourcesAreListedInOrdinalOrder()
    {
        using var ws = new TempWorkspace();
        const string testProject = "<Project><ItemGroup><PackageReference Include=\"Microsoft.NET.Test.Sdk\" /></ItemGroup></Project>";
        var b = ws.Write("tests/B.Tests/B.Tests.csproj", testProject);
        var a = ws.Write("tests/A.Tests/A.Tests.csproj", testProject);
        ws.Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        var z = Touch(ws, "src/App/Z.cs");
        var first = Touch(ws, "src/App/A.cs");

        var workspace = ProjectDiscovery.Discover(ws.Root);

        Assert.Equal([a, b], workspace.TestProjects);
        Assert.Equal([first, z], workspace.SourceFiles);
    }

    [Fact]
    public void AFileOfANestedProjectBelongsToTheNestedProject()
    {
        using var ws = new TempWorkspace();
        ws.Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        var nested = ws.Write("src/App/Sub/Sub.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        var outer = Touch(ws, "src/App/O.cs");
        var inner = Touch(ws, "src/App/Sub/S.cs");

        var workspace = ProjectDiscovery.Discover(ws.Root);

        Assert.Equal(nested, workspace.SourceProjectByFile[inner]);
        Assert.Equal(ws.Path_("src/App/App.csproj"), workspace.SourceProjectByFile[outer]);
    }

    [Fact]
    public void TwoProjectsInTheSameFolderLeaveTheFirstOneAsTheOwnerOfTheFiles()
    {
        using var ws = new TempWorkspace();
        var first = ws.Write("src/App/A.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        ws.Write("src/App/B.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        var source = Touch(ws, "src/App/Code.cs");

        var workspace = ProjectDiscovery.Discover(ws.Root);

        Assert.Equal(first, workspace.SourceProjectByFile[source]);
    }

    [Fact]
    public void ReportsAreListedInOrdinalOrder()
    {
        using var ws = new TempWorkspace();
        var second = ws.Write("b/TestResults/g/coverage.cobertura.xml", "<coverage/>");
        var first = ws.Write("a/TestResults/g/coverage.cobertura.xml", "<coverage/>");

        Assert.Equal([first, second], CoverageReportFinder.LatestRuns(ws.Root));
        Assert.Equal([first, second], CoverageReportFinder.All(ws.Root));
        Assert.Equal([first, second], CoverageReportFinder.ModifiedSince(ws.Root, DateTime.UtcNow.AddHours(-1)));
    }

    [Fact]
    public void AReportWrittenExactlyAtTheStartCountsAsFreshFromACommand()
    {
        using var ws = new TempWorkspace();
        var since = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var onTime = ws.Write("a/coverage.cobertura.xml", "<coverage/>");
        var tooEarly = ws.Write("b/coverage.cobertura.xml", "<coverage/>");
        File.SetLastWriteTimeUtc(onTime, since);
        File.SetLastWriteTimeUtc(tooEarly, since.AddSeconds(-1));

        Assert.Equal([onTime], CoverageReportFinder.ModifiedSince(ws.Root, since));
    }

    [Fact]
    public void AReportExactlyTenMinutesOlderStillBelongsToTheRun()
    {
        using var ws = new TempWorkspace();
        var newest = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var edge = ws.Write("T/TestResults/a/coverage.cobertura.xml", "<coverage/>");
        var latest = ws.Write("T/TestResults/b/coverage.cobertura.xml", "<coverage/>");
        var older = ws.Write("T/TestResults/c/coverage.cobertura.xml", "<coverage/>");
        File.SetLastWriteTimeUtc(latest, newest);
        File.SetLastWriteTimeUtc(edge, newest.AddMinutes(-10));
        File.SetLastWriteTimeUtc(older, newest.AddMinutes(-10).AddSeconds(-1));

        Assert.Equal([edge, latest], CoverageReportFinder.LatestRuns(ws.Root));
    }

    [Fact]
    public void GitIsAskedForTheRepositoryRootThenForTheStatus()
    {
        var runner = new FakeProcessRunner((_, args, _) => new ProcessResult(0, args[0] == "rev-parse" ? "/repo\n" : "", ""));

        new ChangedFiles(runner).Find("/repo/sub");

        Assert.Equal(["rev-parse", "--show-toplevel"], runner.Calls[0].Arguments);
        Assert.Equal(["status", "--porcelain", "-uall"], runner.Calls[1].Arguments);
        Assert.All(runner.Calls, call => Assert.Equal("git", call.File));
    }

    [Fact]
    public void ABlankSourceInACoberturaReportIsIgnored()
    {
        var report = XDocument.Parse(
            "<coverage><sources><source>  </source></sources><packages><package><classes>"
            + "<class filename=\"x.cs\"><lines><line number=\"1\" hits=\"1\"/></lines></class></classes></package></packages></coverage>");

        Assert.Equal(0, CoberturaParser.Parse(report).FileCount);
    }

    [Fact]
    public void ALineWithoutHitsCountsAsNotHitAndAClassWithoutFilenameIsSkipped()
    {
        var file = File1;
        var report = XDocument.Parse(
            "<coverage><packages><package><classes>"
            + $"<class filename=\"{file}\"><lines><line number=\"1\"/><line number=\"2\" hits=\"1\"/></lines></class>"
            + "<class><lines><line number=\"9\" hits=\"1\"/></lines></class>"
            + "</classes></package></packages></coverage>");

        var map = CoberturaParser.Parse(report);

        Assert.Equal(1, map.FileCount);
        Assert.Equal(0.5, map.CoverageOf(file, 1, 2));
    }

    [Fact]
    public void ARelativeFilenameResolvesToTheSourceFolderThatHasItOtherwiseToTheFirst()
    {
        using var ws = new TempWorkspace();
        var empty = ws.Path_("one");
        var holding = Path.GetDirectoryName(ws.Write("two/x.cs", "class X { }"))!;
        Directory.CreateDirectory(empty);

        CoverageMap Parse(params string[] sources) => CoberturaParser.Parse(XDocument.Parse(
            "<coverage><sources>" + string.Concat(sources.Select(s => $"<source>{s}</source>")) + "</sources><packages><package><classes>"
            + "<class filename=\"x.cs\"><lines><line number=\"1\" hits=\"1\"/></lines></class></classes></package></packages></coverage>"));

        Assert.True(Parse(empty, holding).Contains(Path.Combine(holding, "x.cs")));
        Assert.True(Parse(empty, ws.Path_("three")).Contains(Path.Combine(empty, "x.cs")));
    }

    [Fact]
    public void EveryTopLevelStatementAddsItsDecisionsToTheSingleEntry()
    {
        var methods = ComplexityCalculator.Analyze("if (args.Length > 0) { }\nif (args.Length > 1) { }\n", "Program.cs");

        Assert.Equal(3, Assert.Single(methods).Complexity);
    }

    [Fact]
    public void PathsAreCaseInsensitiveOnWindowsOnly()
    {
        Assert.Equal(OperatingSystem.IsWindows(), PathComparison.Comparer.Equals("Foo.cs", "foo.cs"));
        Assert.Equal(OperatingSystem.IsWindows(), "Foo.cs".StartsWith("foo", PathComparison.Comparison));
    }

    [Fact]
    public void ErrorsNameTheOffendingOptions()
    {
        Assert.Equal("Only one path can be given, got: a, b", Assert.Throws<OptionsException>(() => Options.Parse(["a", "b"])).Message);
        Assert.Equal(
            "Choose one coverage source, got: --no-coverage, --use-existing-coverage",
            Assert.Throws<OptionsException>(() => Options.Parse(["--no-coverage", "--use-existing-coverage"])).Message);
    }
}

public class ProcessRunnerTests
{
    private static readonly SystemProcessRunner Runner = new();

    [Fact]
    public void StandardOutputAndTheExitCodeAreCapturedWithoutAStrayBlankLine()
    {
        var result = Runner.Run("dotnet", ["--version"], Path.GetTempPath());

        Assert.Equal(0, result.ExitCode);
        var output = result.Output.ReplaceLineEndings("\n");
        Assert.EndsWith("\n", output);
        Assert.DoesNotContain("\n\n", output);
        Assert.NotEqual("", output.Trim());
    }

    [Fact]
    public void StandardErrorIsCapturedSeparately()
    {
        var result = Runner.Run("dotnet", ["a-command-that-does-not-exist"], Path.GetTempPath());

        Assert.NotEqual(0, result.ExitCode);
        Assert.NotEqual("", result.Error.Trim());
    }

    [Fact]
    public void AMissingExecutableIsReportedAsNotFound()
    {
        var result = Runner.Run("crapsharp-no-such-executable", [], Path.GetTempPath());

        Assert.Equal(SystemProcessRunner.NotFound, result.ExitCode);
        Assert.Equal("", result.Output);
        Assert.NotEqual("", result.Error);
    }
}
