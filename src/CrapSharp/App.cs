using System.Globalization;
using CrapSharp.Complexity;
using CrapSharp.Coverage;
using CrapSharp.Git;
using CrapSharp.Processes;
using CrapSharp.Projects;
using CrapSharp.Scoring;

namespace CrapSharp;

/// <summary>
/// The whole program minus the process entry point, so it can be tested with fake processes.
/// Exit codes: see <see cref="ExitCodes"/>.
/// </summary>
public sealed class App(IProcessRunner processes, TextWriter output, TextWriter error)
{
    public int Run(string[] args)
    {
        Options options;
        try
        {
            options = Options.Parse(args);
        }
        catch (OptionsException e)
        {
            return Fail($"{e.Message}\nRun crapsharp --help for the usage.");
        }
        if (!options.Help)
            return Run(options);
        output.Write(Options.Usage);
        return ExitCodes.Ok;
    }

    public int Run(Options options)
    {
        if (Invalid(options) is { } problem)
            return Fail(problem);
        var workspace = ProjectDiscovery.Discover(options.Path);
        var (files, earlyExit) = Select(workspace, options);
        if (earlyExit is { } code)
            return code;
        var coverage = new CoverageProvider(processes, error).Obtain(options, workspace);
        return coverage.Succeeded ? Score(options, workspace, files, coverage) : ExitCodes.Error;
    }

    private static string? Invalid(Options options)
    {
        if (!File.Exists(options.Path) && !Directory.Exists(options.Path))
            return $"Path not found: {options.Path}";
        return options.Threshold is not null && options.Mode == CoverageMode.None
            ? "--threshold needs coverage data; remove --no-coverage"
            : null;
    }

    private int Score(Options options, Workspace workspace, IReadOnlyList<string> files, CoverageResult coverage)
    {
        var methods = files.SelectMany(file => ComplexityCalculator.Analyze(File.ReadAllText(file), file)).ToList();
        if (coverage.Map is { } map && !CheckCoverage(methods, workspace, map))
            return ExitCodes.Error;
        var context = new ReportContext(options.Changed, options.Filter, options.Mode, coverage.Reports, files.Count);
        var thresholdExitCode = Report(options, workspace, methods, coverage.Map, context);
        return coverage.RunFailed ? TestsFailed() : thresholdExitCode;
    }

    // ExitCode is set when there is nothing (more) to do: an error, or a selection that is empty on purpose.
    private (IReadOnlyList<string> Files, int? ExitCode) Select(Workspace workspace, Options options)
    {
        if (workspace.SourceFiles.Count == 0)
            return ([], Fail($"no production C# source file found under {workspace.Root}"));
        var files = options.Filter is null
            ? workspace.SourceFiles
            : workspace.SourceFiles.Where(file => MatchesFilter(file, options.Filter)).ToList();
        if (files.Count == 0)
            return ([], Fail($"--filter '{options.Filter}' matches none of the {workspace.SourceFiles.Count} source file(s)"));
        return options.Changed ? KeepChanged(workspace, files) : (files, null);
    }

    // Nothing changed is a legitimate empty selection (success); a typo in --filter is not (see Select).
    private (IReadOnlyList<string> Files, int? ExitCode) KeepChanged(Workspace workspace, IReadOnlyList<string> files)
    {
        try
        {
            var changed = new ChangedFiles(processes).Find(workspace.Root);
            var kept = files.Where(changed.Contains).ToList();
            if (kept.Count > 0)
                return (kept, null);
            error.WriteLine("no changed C# source file to score");
            return (kept, ExitCodes.Ok);
        }
        catch (InvalidOperationException e)
        {
            return ([], Fail(e.Message));
        }
    }

    private static bool MatchesFilter(string file, string fragment) =>
        file.Replace('\\', '/').Contains(fragment.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);

    private int Report(Options options, Workspace workspace, IReadOnlyList<MethodComplexity> methods, CoverageMap? coverage, ReportContext context)
    {
        var reports = MethodReport.Build(methods, coverage);
        output.Write(ReportFormatter.Table(reports));
        if (coverage is not null)
        {
            JsonReportWriter.Write(reports, Path.Combine(workspace.Root, ".metrics", "crap.json"), context);
        }
        return ThresholdExitCode(options.Threshold, reports);
    }

    // Only files with a measurable member count: a file of constants has nothing the coverage could say about it.
    private bool CheckCoverage(IReadOnlyList<MethodComplexity> methods, Workspace workspace, CoverageMap coverage)
    {
        var files = methods.Select(method => method.FilePath).Distinct(PathComparison.Comparer).ToList();
        if (files.Count == 0)
            return true;
        var missing = files.Where(file => !coverage.Contains(file)).ToList();
        if (missing.Count == files.Count)
        {
            error.WriteLine($"error: none of the {files.Count} analyzed file(s) appears in the coverage report. "
                + "Check that the report paths match the sources (deterministic builds can write /_/ paths).");
            return false;
        }
        foreach (var group in missing.GroupBy(file => SourceProject(file, workspace), PathComparison.Comparer))
        {
            var project = Path.GetRelativePath(workspace.Root, group.Key);
            error.WriteLine($"warning: coverage report does not mention {group.Count()} analyzed file(s) in project "
                + $"{project}; their methods count as 0 % covered");
        }
        return true;
    }

    private static string SourceProject(string file, Workspace workspace) =>
        workspace.SourceProjectByFile.TryGetValue(file, out var project) ? project : workspace.Root;

    private int ThresholdExitCode(double? threshold, IReadOnlyList<MethodReport> reports)
    {
        if (threshold is not { } limit)
            return ExitCodes.Ok;
        var failing = ThresholdCheck.Failing(reports, limit);
        if (failing.Count == 0)
            return ExitCodes.Ok;
        var shown = limit.ToString(CultureInfo.InvariantCulture);
        error.WriteLine($"CRAP threshold not met: {failing.Count} method(s) score {shown} or more; every method must score below {shown}");
        return ExitCodes.ThresholdNotMet;
    }

    private int TestsFailed()
    {
        error.WriteLine("The tests or the coverage command failed: the scores above may rest on incomplete coverage");
        return ExitCodes.TestsFailed;
    }

    private int Fail(string message)
    {
        error.WriteLine(message);
        return ExitCodes.Error;
    }
}
