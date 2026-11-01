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
        Assert.Contains("tests failed for App.Tests.csproj (exit 1)", errors);
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
    public void UseExistingKeepsEveryReportOfTheNewestRunSoNoTargetFrameworkIsLost()
    {
        using var ws = new TempWorkspace();
        var source = ws.Path_("Foo.cs");
        var net9 = ws.Write("tests/T/TestResults/a/coverage.cobertura.xml", TempWorkspace.CoberturaXml(source, (1, 1), (2, 0)));
        var net10 = ws.Write("tests/T/TestResults/b/coverage.cobertura.xml", TempWorkspace.CoberturaXml(source, (1, 0), (2, 1)));
        var stale = ws.Write("tests/T/TestResults/c/coverage.cobertura.xml", TempWorkspace.CoberturaXml(source, (3, 1)));
        File.SetLastWriteTimeUtc(stale, DateTime.UtcNow.AddHours(-3));
        File.SetLastWriteTimeUtc(net9, DateTime.UtcNow.AddSeconds(-30));
        File.SetLastWriteTimeUtc(net10, DateTime.UtcNow);

        var (result, _) = Obtain(new Options(ws.Root, [], UseExistingCoverage: true), new Workspace(ws.Root, [], []), new FakeProcessRunner());

        Assert.Equal(2, result.Reports.Count);
        Assert.Equal(1.0, result.Map!.CoverageOf(source, 1, 2));
    }

    [Fact]
    public void ADefaultRunMergesTheReportsOfEveryTargetFrameworkOfATestProject()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();
        var runner = new FakeProcessRunner((_, args, _) =>
        {
            var results = ArgAfter(args, "--results-directory");
            foreach (var (guid, hit) in new[] { ("net9", (6, 1)), ("net10", (7, 1)) })
            {
                var report = Path.Combine(results, guid, "coverage.cobertura.xml");
                Directory.CreateDirectory(Path.GetDirectoryName(report)!);
                File.WriteAllText(report, TempWorkspace.CoberturaXml(source, hit));
            }
            return new ProcessResult(0, "", "");
        });

        var (result, _) = Obtain(new Options(ws.Root, []), ProjectDiscovery.Discover(ws.Root), runner);

        Assert.Equal(2, result.Reports.Count);
        Assert.Equal(1.0, result.Map!.CoverageOf(source, 6, 7));
    }

    [Fact]
    public void ATestProjectThatProducedNoReportAmongSeveralIsNamedInAWarning()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();
        ws.Write("tests/Other.Tests/Other.Tests.csproj", "<Project><ItemGroup><PackageReference Include=\"Microsoft.NET.Test.Sdk\" /></ItemGroup></Project>");
        var runner = new FakeProcessRunner((_, args, _) =>
        {
            if (args[1].EndsWith("Other.Tests.csproj"))
                return new ProcessResult(0, "", "");
            var report = Path.Combine(ArgAfter(args, "--results-directory"), "g", "coverage.cobertura.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(report)!);
            File.WriteAllText(report, TempWorkspace.CoberturaXml(source, (6, 1)));
            return new ProcessResult(0, "", "");
        });

        var (result, errors) = Obtain(new Options(ws.Root, []), ProjectDiscovery.Discover(ws.Root), runner);

        Assert.True(result.Succeeded);
        Assert.Contains("warning: Other.Tests.csproj produced no coverage report", errors);
        Assert.DoesNotContain("App.Tests.csproj produced no coverage report", errors);
    }

    private static (TempWorkspace Workspace, string Source, string Project) MtpSolution(string packages)
    {
        var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();
        File.Delete(ws.Path_("tests/App.Tests/App.Tests.csproj"));
        var project = ws.Write("tests/App.Tests/App.Tests.csproj",
            "<Project><PropertyGroup><OutputType>Exe</OutputType><EnableMSTestRunner>true</EnableMSTestRunner></PropertyGroup>"
            + $"<ItemGroup><PackageReference Include=\"MSTest\" Version=\"4.0.2\" />{packages}</ItemGroup></Project>");
        return (ws, source, project);
    }

    [Theory]
    [InlineData("<PackageReference Include=\"Microsoft.Testing.Extensions.CodeCoverage\" Version=\"18.1.0\" />",
        "--coverage", "--coverage-output-format")]
    [InlineData("<PackageReference Include=\"coverlet.MTP\" Version=\"10.1.0\" />",
        "--coverlet", "--coverlet-output-format")]
    public void AMicrosoftTestingPlatformProjectIsRunWithItsOwnCoverageExtension(string packages, string enable, string format)
    {
        var (ws, source, project) = MtpSolution(packages);
        using var _ = ws;
        var runner = new FakeProcessRunner((_, args, _) =>
        {
            var report = Path.Combine(ArgAfter(args, "--results-directory"), "x.cobertura.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(report)!);
            File.WriteAllText(report, TempWorkspace.CoberturaXml(source, (6, 1), (7, 0)));
            return new ProcessResult(0, "", "");
        });

        var (result, _) = Obtain(new Options(ws.Root, []), ProjectDiscovery.Discover(ws.Root), runner);

        var call = Assert.Single(runner.Calls);
        Assert.Equal("dotnet", call.File);
        // `dotnet run` works on every SDK, unlike `dotnet test` which needs an opt-in in global.json on .NET 10.
        Assert.Equal(["run", "--project", project, "--", enable, format, "cobertura"], call.Arguments[..7]);
        Assert.True(result.Succeeded);
        Assert.Equal(0.5, result.Map!.CoverageOf(source, 6, 7));
    }

    [Fact]
    public void AMicrosoftTestingPlatformProjectWithoutACoverageExtensionIsNamedAndNotRun()
    {
        var (ws, _, _) = MtpSolution("");
        using var _ = ws;
        var runner = new FakeProcessRunner();

        var (result, errors) = Obtain(new Options(ws.Root, []), ProjectDiscovery.Discover(ws.Root), runner);

        Assert.False(result.Succeeded);
        Assert.Empty(runner.Calls);
        Assert.Contains("App.Tests.csproj uses Microsoft.Testing.Platform", errors);
        Assert.Contains("Microsoft.Testing.Extensions.CodeCoverage", errors);
        Assert.Contains("coverlet.MTP", errors);
    }

    [Fact]
    public void AnXunitV3ProjectThatKeepsItsOwnRunnerIsToldHowToOptIn()
    {
        using var ws = new TempWorkspace();
        ws.CreateSolution();
        File.Delete(ws.Path_("tests/App.Tests/App.Tests.csproj"));
        ws.Write("tests/App.Tests/App.Tests.csproj",
            "<Project><PropertyGroup><OutputType>Exe</OutputType></PropertyGroup><ItemGroup>"
            + "<PackageReference Include=\"xunit.v3\" Version=\"4.0.2\" />"
            + "<PackageReference Include=\"Microsoft.Testing.Extensions.CodeCoverage\" Version=\"18.1.0\" /></ItemGroup></Project>");
        var runner = new FakeProcessRunner();

        var (result, errors) = Obtain(new Options(ws.Root, []), ProjectDiscovery.Discover(ws.Root), runner);

        Assert.False(result.Succeeded);
        Assert.Empty(runner.Calls);
        Assert.Contains("<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>", errors);
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

    [Fact]
    public void TestsAreRunWithTheirResultsUnderMetricsCoverageAndAnnounced()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();
        var runner = TestRunnerWriting(source);

        var (_, errors) = Obtain(new Options(ws.Root, []), ProjectDiscovery.Discover(ws.Root), runner);

        var results = ArgAfter(runner.Calls.Single().Arguments, "--results-directory");
        Assert.Equal(Path.Combine(ws.Root, ".metrics", "coverage", "0"), results);
        Assert.Contains("Running tests with coverage: App.Tests.csproj", errors);
    }

    [Fact]
    public void TheRunIsFlaggedFailedWhenSeveralTestProjectsFail()
    {
        using var ws = new TempWorkspace();
        var (source, _) = ws.CreateSolution();
        ws.Write("tests/Other.Tests/Other.Tests.csproj", "<Project><ItemGroup><PackageReference Include=\"Microsoft.NET.Test.Sdk\" /></ItemGroup></Project>");
        var runner = TestRunnerWriting(source, exitCode: 1);

        var (result, _) = Obtain(new Options(ws.Root, []), ProjectDiscovery.Discover(ws.Root), runner);

        Assert.Equal(2, runner.Calls.Count);
        Assert.True(result.RunFailed);
    }

    [Fact]
    public void ASilentTestProjectWarningExplainsTheZeroPercent()
    {
        var (ws, source, _) = MtpSolution("<PackageReference Include=\"coverlet.MTP\" Version=\"10.1.0\" />");
        using var _ = ws;
        ws.Write("tests/Silent.Tests/Silent.Tests.csproj", "<Project><ItemGroup><PackageReference Include=\"Microsoft.NET.Test.Sdk\" /></ItemGroup></Project>");
        var runner = new FakeProcessRunner((_, args, _) =>
        {
            if (args.Any(a => a.EndsWith("Silent.Tests.csproj")))
                return new ProcessResult(0, "", "");
            var report = Path.Combine(ArgAfter(args, "--results-directory"), "x.cobertura.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(report)!);
            File.WriteAllText(report, TempWorkspace.CoberturaXml(source, (6, 1)));
            return new ProcessResult(0, "", "");
        });

        var (_, errors) = Obtain(new Options(ws.Root, []), ProjectDiscovery.Discover(ws.Root), runner);

        Assert.Contains("warning: Silent.Tests.csproj produced no coverage report; what only its tests cover is scored 0 %", errors);
    }

    [Fact]
    public void AMicrosoftTestingPlatformProjectWithoutAnExtensionIsNotRunAndIsNotAFailedRun()
    {
        var (ws, source, _) = MtpSolution("");
        using var _ = ws;
        ws.Write("tests/Plain.Tests/Plain.Tests.csproj", "<Project><ItemGroup><PackageReference Include=\"Microsoft.NET.Test.Sdk\" /></ItemGroup></Project>");
        var runner = TestRunnerWriting(source);

        var (result, errors) = Obtain(new Options(ws.Root, []), ProjectDiscovery.Discover(ws.Root), runner);

        Assert.True(result.Succeeded);
        Assert.False(result.RunFailed);
        Assert.Contains("cannot write a Cobertura report; not run", errors);
        Assert.DoesNotContain("Running tests with coverage: App.Tests.csproj", errors);
    }

    [Fact]
    public void ACoverageCommandIsAnnouncedAndMustLeaveAReport()
    {
        using var ws = new TempWorkspace();

        var (result, errors) = Obtain(new Options(ws.Root, [], CoverageCommand: "my-cover"), new Workspace(ws.Root, [], []), new FakeProcessRunner());

        Assert.False(result.Succeeded);
        Assert.Contains("Running coverage command: my-cover", errors);
        Assert.Contains("The coverage command produced no Cobertura report", errors);
    }

    [Fact]
    public void TheCoverageCommandReachesTheShellOfTheOperatingSystemUnchanged()
    {
        using var ws = new TempWorkspace();
        string? script = null;
        var runner = new FakeProcessRunner((_, args, _) =>
        {
            if (OperatingSystem.IsWindows())
                script = File.ReadAllText(args.Last());
            return new ProcessResult(0, "", "");
        });

        Obtain(new Options(ws.Root, [], CoverageCommand: "my-cover --all"), new Workspace(ws.Root, [], []), runner);

        var call = Assert.Single(runner.Calls);
        if (OperatingSystem.IsWindows())
        {
            Assert.Equal("cmd", call.File);
            Assert.Equal("/c", call.Arguments[0]);
            Assert.Equal("@echo off\r\nmy-cover --all\r\n", script);
        }
        else
        {
            Assert.Equal("sh", call.File);
            Assert.Equal(["-c", "my-cover --all"], call.Arguments);
        }
        Assert.Equal(ws.Root, call.WorkingDirectory);
    }
}
