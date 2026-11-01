using System.Xml;
using CrapSharp.Processes;
using CrapSharp.Projects;

namespace CrapSharp.Coverage;

/// <summary>
/// Succeeded=false means an error was already reported; Map=null with Succeeded=true means "no coverage wanted".
/// RunFailed means `dotnet test` or the coverage command failed: the map may miss what the failing tests would have covered.
/// </summary>
public sealed record CoverageResult(CoverageMap? Map, bool Succeeded, bool RunFailed = false)
{
    /// <summary>The Cobertura reports the map was merged from.</summary>
    public IReadOnlyList<string> Reports { get; init; } = [];

    public static readonly CoverageResult NotWanted = new(null, true);
    public static readonly CoverageResult Failed = new(null, false);
}

/// <summary>Gets the coverage the way the options ask for it: run the tests, reuse reports, run a custom command...</summary>
public sealed class CoverageProvider(IProcessRunner processes, TextWriter error)
{
    private const int LinesOfFailureOutput = 15;

    private const string NoReportFromDotnetTest =
        "The test run produced no coverage report. VSTest projects need the coverlet.collector package; "
        + "Microsoft.Testing.Platform projects need Microsoft.Testing.Extensions.CodeCoverage or coverlet.MTP";

    public CoverageResult Obtain(Options options, Workspace workspace) => options.Mode switch
    {
        CoverageMode.None => CoverageResult.NotWanted,
        CoverageMode.Explicit => Load(options.CoverageReports, "No coverage report given"),
        CoverageMode.UseExisting => Load(CoverageReportFinder.LatestRuns(workspace.Root),
            $"No existing coverage report found under {workspace.Root}; run the tests with coverage first"),
        CoverageMode.Command => RunCommand(options.CoverageCommand!, workspace),
        _ => RunTests(workspace),
    };

    private CoverageResult RunTests(Workspace workspace)
    {
        if (workspace.TestProjects.Count == 0)
            return Fail("No test project found; pass --coverage <report>, --use-existing-coverage or --no-coverage");
        var resultsRoot = Path.Combine(workspace.Root, ".metrics", "coverage");
        if (Directory.Exists(resultsRoot))
            Directory.Delete(resultsRoot, recursive: true);
        var runFailed = false;
        for (var i = 0; i < workspace.TestProjects.Count; i++)
            runFailed |= RunTestProject(workspace.TestProjects[i], Path.Combine(resultsRoot, i.ToString()), workspace.Root);
        WarnAboutSilentProjects(workspace.TestProjects, resultsRoot);
        return Load(CoverageReportFinder.All(resultsRoot), NoReportFromDotnetTest) with { RunFailed = runFailed };
    }

    // A test project that wrote no report (no collector, tests skipped) leaves what only it covers at 0 %.
    private void WarnAboutSilentProjects(IReadOnlyList<string> testProjects, string resultsRoot)
    {
        for (var i = 0; i < testProjects.Count; i++)
            if (CoverageReportFinder.All(Path.Combine(resultsRoot, i.ToString())).Count == 0)
                error.WriteLine($"warning: {Path.GetFileName(testProjects[i])} produced no coverage report; "
                    + "what only its tests cover is scored 0 % (does it reference coverlet.collector?)");
    }

    /// <summary>True when the run failed.</summary>
    private bool RunTestProject(string project, string resultsDirectory, string root)
    {
        var runner = TestRunners.Of(File.ReadAllText(project));
        if (runner == TestRunner.MtpWithoutCoverage)
        {
            error.WriteLine($"warning: {Path.GetFileName(project)} uses Microsoft.Testing.Platform but references neither "
                + "Microsoft.Testing.Extensions.CodeCoverage nor coverlet.MTP, so it cannot write a Cobertura report; not run");
            return false;
        }
        error.WriteLine($"Running tests with coverage: {Path.GetFileName(project)}");
        var result = processes.Run("dotnet", TestRunners.Arguments(runner, project, resultsDirectory), root);
        return WarnIfFailed(result, $"tests failed for {Path.GetFileName(project)}");
    }

    private CoverageResult RunCommand(string command, Workspace workspace)
    {
        var started = DateTime.UtcNow.AddSeconds(-1);
        error.WriteLine($"Running coverage command: {command}");
        var failed = WarnIfFailed(RunShell(command, workspace.Root), "The coverage command failed");
        return Load(CoverageReportFinder.ModifiedSince(workspace.Root, started),
            "The coverage command produced no Cobertura report") with { RunFailed = failed };
    }

    // cmd.exe mangles the quotes of a command passed as an argument, so on Windows the command goes through a temp script.
    private ProcessResult RunShell(string command, string root)
    {
        if (!OperatingSystem.IsWindows())
            return processes.Run("sh", ["-c", command], root);
        var script = Path.Combine(Path.GetTempPath(), $"crapsharp-{Guid.NewGuid():N}.cmd");
        File.WriteAllText(script, "@echo off\r\n" + command + "\r\n");
        try
        {
            return processes.Run("cmd", ["/c", script], root);
        }
        finally
        {
            File.Delete(script);
        }
    }

    /// <summary>True (after printing the end of the output) when the process failed.</summary>
    private bool WarnIfFailed(ProcessResult result, string message)
    {
        if (result.ExitCode == 0)
            return false;
        var tail = result.Combined.Split('\n').TakeLast(LinesOfFailureOutput);
        error.WriteLine($"warning: {message} (exit {result.ExitCode}); coverage may be incomplete");
        error.WriteLine(string.Join('\n', tail));
        return true;
    }

    private CoverageResult Load(IReadOnlyList<string> reports, string noneMessage)
    {
        if (reports.Count == 0)
            return Fail(noneMessage);
        try
        {
            var merged = new CoverageMap();
            foreach (var report in reports)
                merged.Merge(CoberturaParser.ParseFile(report));
            return new CoverageResult(merged, true) { Reports = reports };
        }
        catch (Exception e) when (e is IOException or XmlException)
        {
            return Fail($"Cannot read coverage report: {e.Message}");
        }
    }

    private CoverageResult Fail(string message)
    {
        error.WriteLine(message);
        return CoverageResult.Failed;
    }
}
