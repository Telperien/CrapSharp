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
        if (!File.Exists(options.Path) && !Directory.Exists(options.Path))
            return Fail($"Path not found: {options.Path}");
        if (options.Threshold is not null && options.Mode == CoverageMode.None)
            return Fail("--threshold needs coverage data; remove --no-coverage");
        var workspace = ProjectDiscovery.Discover(options.Path);
        var files = SelectFiles(workspace, options);
        if (files is null)
            return ExitCodes.Error;
        var coverage = new CoverageProvider(processes, error).Obtain(options, workspace);
        if (!coverage.Succeeded)
            return ExitCodes.Error;
        if (coverage.Map is { } map && !CheckCoverage(files, workspace, map))
            return ExitCodes.Error;
        var thresholdExitCode = Report(options, workspace, files, coverage.Map);
        return coverage.RunFailed ? TestsFailed() : thresholdExitCode;
    }

    private IReadOnlyList<string>? SelectFiles(Workspace workspace, Options options)
    {
        IReadOnlyList<string> files = workspace.SourceFiles;
        if (options.Filter is not null)
            files = files.Where(file => MatchesFilter(file, options.Filter)).ToList();
        if (!options.Changed)
            return files;
        try
        {
            var changed = new ChangedFiles(processes).Find(workspace.Root);
            return files.Where(changed.Contains).ToList();
        }
        catch (InvalidOperationException e)
        {
            error.WriteLine(e.Message);
            return null;
        }
    }

    private static bool MatchesFilter(string file, string fragment) =>
        file.Replace('\\', '/').Contains(fragment.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);

    private int Report(Options options, Workspace workspace, IReadOnlyList<string> files, CoverageMap? coverage)
    {
        var methods = files.SelectMany(file => ComplexityCalculator.Analyze(File.ReadAllText(file), file)).ToList();
        var reports = MethodReport.Build(methods, coverage);
        output.Write(ReportFormatter.Table(reports));
        if (coverage is not null)
        {
            JsonReportWriter.Write(reports, Path.Combine(workspace.Root, ".metrics", "crap.json"));
        }
        return ThresholdExitCode(options.Threshold, reports);
    }

    private bool CheckCoverage(IReadOnlyList<string> files, Workspace workspace, CoverageMap coverage)
    {
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
