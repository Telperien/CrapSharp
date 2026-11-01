using CrapSharp.Coverage;
using CrapSharp.Processes;
using CrapSharp.Projects;
using Xunit;

namespace CrapSharp.Tests;

public class CoverageProviderTests
{
    private static string ArgAfter(IReadOnlyList<string> args, string name)
    {
        for (var i = 0; i < args.Count - 1; i++)
            if (args[i] == name)
                return args[i + 1];
        throw new InvalidOperationException($"{name} not passed");
    }

    private static FakeProcessRunner TestRunnerWriting(string source, int exitCode = 0) => new((_, args, _) =>
    {
        var report = Path.Combine(ArgAfter(args, "--results-directory"), "guid", "coverage.cobertura.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(report)!);
        File.WriteAllText(report, TempWorkspace.CoberturaXml(source, (6, 1), (7, 0)));
        return new ProcessResult(exitCode, "some test output\n", "");
    });

    private static (CoverageResult Result, string Errors) Obtain(Options options, Workspace workspace, IProcessRunner runner)
    {
        var errors = new StringWriter();
        return (new CoverageProvider(runner, errors).Obtain(options, workspace), errors.ToString());
    }

    [Fact]
    public void NoCoverageModeReturnsNothingAndRunsNothing()
    {
        var runner = new FakeProcessRunner();
        var (result, _) = Obtain(new Options(".", [], NoCoverage: true), new Workspace(".", [], []), runner);
        Assert.True(result.Succeeded);
        Assert.Null(result.Map);
        Assert.Empty(runner.Calls);
    }

    [Fact]
    public void DefaultModeRunsDotnetTestWithCoverletOnEachTestProject()
    {
        using var ws = new TempWorkspace();
        var (source, testProject) = ws.CreateSolution();
        var runner = TestRunnerWriting(source);

        var (result, _) = Obtain(new Options(ws.Root, []), ProjectDiscovery.Discover(ws.Root), runner);

        var call = Assert.Single(runner.Calls);
        Assert.Equal("dotnet", call.File);
        Assert.Equal(["test", testProject, "--collect:XPlat Code Coverage"], call.Arguments[..3]);
        Assert.Equal(ws.Root, call.WorkingDirectory);
        Assert.Equal(0.5, result.Map!.CoverageOf(source, 6, 7));
        Assert.False(result.RunFailed);
    }

    [Fact]
    public void FailingTestsAreFlaggedButTheirCoverageIsStillUsed()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();

        var (result, errors) = Obtain(new Options(ws.Root, []), ProjectDiscovery.Discover(ws.Root), TestRunnerWriting(source, exitCode: 1));

        Assert.True(result.Succeeded);
        Assert.True(result.RunFailed);
        Assert.Equal(0.5, result.Map!.CoverageOf(source, 6, 7));
        Assert.Contains("dotnet test failed for App.Tests.csproj (exit 1)", errors);
        Assert.Contains("some test output", errors);
    }

    [Fact]
    public void StaleReportsOfAPreviousRunAreDeletedBeforeTheNewRun()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();
        var stale = ws.Write(".metrics/coverage/0/old/coverage.cobertura.xml", TempWorkspace.CoberturaXml(source, (6, 1), (7, 1)));

        var (result, _) = Obtain(new Options(ws.Root, []), ProjectDiscovery.Discover(ws.Root), TestRunnerWriting(source));

        Assert.False(File.Exists(stale));
        Assert.Equal(0.5, result.Map!.CoverageOf(source, 6, 7));
    }

    [Fact]
    public void NoTestProjectIsAnError()
    {
        var (result, errors) = Obtain(new Options(".", []), new Workspace(".", [], []), new FakeProcessRunner());
        Assert.False(result.Succeeded);
        Assert.Contains("No test project found", errors);
    }

    [Fact]
    public void NoReportAfterTheRunIsAnError()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();

        var (result, errors) = Obtain(new Options(ws.Root, []), ProjectDiscovery.Discover(ws.Root), new FakeProcessRunner());

        Assert.False(result.Succeeded);
        Assert.Contains("coverlet.collector", errors);
        Assert.Contains("Microsoft.Testing.Platform", errors);
    }

    [Fact]
    public void AFailingCoverageCommandIsFlagged()
    {
        using var ws = new TempWorkspace();
        var source = ws.Path_("Foo.cs");
        var runner = new FakeProcessRunner((_, _, _) =>
        {
            ws.Write("out/coverage.cobertura.xml", TempWorkspace.CoberturaXml(source, (1, 1)));
            return new ProcessResult(2, "", "boom");
        });

        var (result, errors) = Obtain(new Options(ws.Root, [], CoverageCommand: "cover"), new Workspace(ws.Root, [], []), runner);

        Assert.True(result.Succeeded);
        Assert.True(result.RunFailed);
        Assert.Contains("The coverage command failed (exit 2)", errors);
    }

    [Fact]
    public void ExplicitReportsAreMerged()
    {
        using var ws = new TempWorkspace();
        var source = ws.Path_("Foo.cs");
        var first = ws.Write("1.xml", TempWorkspace.CoberturaXml(source, (1, 1)));
        var second = ws.Write("2.xml", TempWorkspace.CoberturaXml(source, (2, 0)));

        var (result, _) = Obtain(new Options(".", [first, second]), new Workspace(".", [], []), new FakeProcessRunner());

        Assert.Equal(0.5, result.Map!.CoverageOf(source, 1, 2));
    }

    [Fact]
    public void AnUnreadableReportIsReportedNotThrown()
    {
        using var ws = new TempWorkspace();
        var broken = ws.Write("bad.xml", "<not-closed");

        var (result, errors) = Obtain(new Options(".", [broken]), new Workspace(".", [], []), new FakeProcessRunner());

        Assert.False(result.Succeeded);
        Assert.Contains("Cannot read coverage report", errors);
    }

    [Fact]
    public void UseExistingTakesTheNewestReportOfEachRunOnly()
    {
        using var ws = new TempWorkspace();
        var source = ws.Path_("Foo.cs");
        var older = ws.Write("tests/T/TestResults/a/coverage.cobertura.xml", TempWorkspace.CoberturaXml(source, (1, 0)));
        var newer = ws.Write("tests/T/TestResults/b/coverage.cobertura.xml", TempWorkspace.CoberturaXml(source, (1, 1)));
        File.SetLastWriteTimeUtc(older, DateTime.UtcNow.AddHours(-1));
        File.SetLastWriteTimeUtc(newer, DateTime.UtcNow);

        var (result, _) = Obtain(new Options(ws.Root, [], UseExistingCoverage: true), new Workspace(ws.Root, [], []), new FakeProcessRunner());

        Assert.Equal(1.0, result.Map!.CoverageOf(source, 1, 1));
    }

    [Fact]
    public void UseExistingWithoutAnyReportIsAnError()
    {
        using var ws = new TempWorkspace();
        var (result, errors) = Obtain(new Options(ws.Root, [], UseExistingCoverage: true), new Workspace(ws.Root, [], []), new FakeProcessRunner());
        Assert.False(result.Succeeded);
        Assert.Contains("No existing coverage report", errors);
    }

    [Fact]
    public void CoverageCommandIsRunThroughTheShellAndItsFreshReportsAreUsed()
    {
        using var ws = new TempWorkspace();
        var source = ws.Path_("Foo.cs");
        ws.Write("old/coverage.cobertura.xml", TempWorkspace.CoberturaXml(source, (1, 0)));
        File.SetLastWriteTimeUtc(ws.Path_("old/coverage.cobertura.xml"), DateTime.UtcNow.AddHours(-1));
        var commandSeen = "";
        var runner = new FakeProcessRunner((_, args, _) =>
        {
            // On Windows the command is in a temp script that only exists during the call.
            commandSeen = OperatingSystem.IsWindows() ? File.ReadAllText(args.Last()) : args.Last();
            ws.Write("out/coverage.cobertura.xml", TempWorkspace.CoberturaXml(source, (1, 1)));
            return new ProcessResult(0, "", "");
        });

        var (result, _) = Obtain(new Options(ws.Root, [], CoverageCommand: "my-cover \"--all\""), new Workspace(ws.Root, [], []), runner);

        Assert.Contains("my-cover \"--all\"", commandSeen);
        Assert.Equal(1.0, result.Map!.CoverageOf(source, 1, 1));
        if (OperatingSystem.IsWindows())
            Assert.False(File.Exists(runner.Calls.Single().Arguments.Last()));
    }
}
