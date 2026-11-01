using System.Globalization;

namespace CrapSharp;

/// <summary>Where the coverage comes from. Giving several coverage options is an error.</summary>
public enum CoverageMode
{
    /// <summary>--no-coverage: complexity only.</summary>
    None,
    /// <summary>--coverage report.xml (repeatable).</summary>
    Explicit,
    /// <summary>--coverage-command "cmd": run a custom command, use the Cobertura reports it wrote.</summary>
    Command,
    /// <summary>--use-existing-coverage: reuse the reports left by an earlier run.</summary>
    UseExisting,
    /// <summary>Default: run `dotnet test` with coverlet on every test project.</summary>
    Run,
}

/// <summary>A command line the tool cannot understand. The message is meant for the user.</summary>
public sealed class OptionsException(string message) : Exception(message);

public sealed record Options(
    string Path,
    IReadOnlyList<string> CoverageReports,
    double? Threshold = null,
    bool NoCoverage = false,
    bool UseExistingCoverage = false,
    bool Changed = false,
    string? CoverageCommand = null,
    string? Filter = null,
    bool Help = false,
    IReadOnlyList<string>? exclude = null)
{
    /// <summary>--exclude fragments: files whose path contains any of them are left out.</summary>
    public IReadOnlyList<string> Exclude { get; } = exclude ?? [];


    public const string Usage = """
        Usage: crapsharp [path] [options]

        Scores every C# method, worst first: CRAP = CC^2 * (1 - coverage)^3 + CC.
        path: a folder, .sln, .csproj or .cs file (default: the current folder).

        Coverage (default: run `dotnet test` with coverlet on every test project; give at most one of these):
          --coverage <report.xml>     use this Cobertura report (repeatable); run nothing
          --use-existing-coverage     reuse the reports left by an earlier test run
          --coverage-command "<cmd>"  run this command and use the Cobertura reports it writes
          --no-coverage               complexity only

        Selection:
          --changed                   only the .cs files git sees as modified, added or untracked
                                      (none changed: exit 0, nothing scored)
          --filter <fragment>         only the files whose path contains the fragment
                                      (no match: exit 1)
          --exclude <fragment>        leave out the files whose path contains the fragment (repeatable)
                                      With any of these, the JSON says its scope is partial.

          --threshold <N>             exit code 2 unless every method scores below N (N finite, above 0)
          -h, --help                  show this help

        Exit codes: 0 ok, 1 error, 2 threshold not met, 3 tests or coverage command failed.
        Sources absent from a partial coverage report score 0% (with a warning per project).
        If no analyzed source file matches the coverage report, the run exits with code 1.
        The report is printed on stdout and, with coverage, written to <root>/.metrics/crap.json.

        """;

    private static readonly HashSet<string> Flags =
        ["--no-coverage", "--use-existing-coverage", "--changed", "--help", "-h"];

    private static readonly string[] CoverageSourceOptions =
        ["--no-coverage", "--coverage", "--coverage-command", "--use-existing-coverage"];

    private static readonly HashSet<string> ValueOptions =
        ["--coverage", "--threshold", "--coverage-command", "--filter", "--exclude"];

    public CoverageMode Mode
    {
        get
        {
            if (NoCoverage)
                return CoverageMode.None;
            if (CoverageReports.Count > 0)
                return CoverageMode.Explicit;
            if (CoverageCommand is not null)
                return CoverageMode.Command;
            return UseExistingCoverage ? CoverageMode.UseExisting : CoverageMode.Run;
        }
    }

    /// <summary>
    /// Throws OptionsException on an unknown option, a missing value, a second path, several coverage sources
    /// or a threshold that is not a finite number above 0.
    /// </summary>
    public static Options Parse(string[] args)
    {
        var flags = new HashSet<string>();
        var values = new Dictionary<string, List<string>>();
        var positionals = new List<string>();
        Tokenize(args, flags, values, positionals);
        if (positionals.Count > 1)
            throw new OptionsException($"Only one path can be given, got: {string.Join(", ", positionals)}");
        RejectSeveralCoverageSources(flags, values);
        return new Options(
            positionals.SingleOrDefault() ?? ".",
            values.GetValueOrDefault("--coverage") ?? [],
            ThresholdOf(values),
            flags.Contains("--no-coverage"),
            flags.Contains("--use-existing-coverage"),
            flags.Contains("--changed"),
            LastValue(values, "--coverage-command"),
            LastValue(values, "--filter"),
            flags.Contains("--help") || flags.Contains("-h"),
            values.GetValueOrDefault("--exclude"));
    }

    private static void Tokenize(
        string[] args, HashSet<string> flags, Dictionary<string, List<string>> values, List<string> positionals)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (Flags.Contains(args[i]))
                flags.Add(args[i]);
            else if (ValueOptions.Contains(args[i]))
                AddValue(values, args[i], ValueAfter(args, ++i));
            else if (args[i].StartsWith('-'))
                throw new OptionsException($"Unknown option: {args[i]}");
            else
                positionals.Add(args[i]);
        }
    }

    // A known option right after a valued one means the value was forgotten (a value may still start with '-').
    private static string ValueAfter(string[] args, int index) =>
        index < args.Length && !IsOption(args[index])
            ? args[index]
            : throw new OptionsException($"{args[index - 1]} expects a value");

    private static bool IsOption(string arg) => Flags.Contains(arg) || ValueOptions.Contains(arg);

    // Picking one silently could hide the coverage the user meant to gate on.
    private static void RejectSeveralCoverageSources(HashSet<string> flags, Dictionary<string, List<string>> values)
    {
        var given = CoverageSourceOptions.Where(option => flags.Contains(option) || values.ContainsKey(option)).ToList();
        if (given.Count > 1)
            throw new OptionsException($"Choose one coverage source, got: {string.Join(", ", given)}");
    }

    private static void AddValue(Dictionary<string, List<string>> values, string option, string value)
    {
        if (!values.TryGetValue(option, out var list))
            values[option] = list = [];
        list.Add(value);
    }

    private static string? LastValue(Dictionary<string, List<string>> values, string option) =>
        values.TryGetValue(option, out var list) ? list[^1] : null;

    private static double? ThresholdOf(Dictionary<string, List<string>> values)
    {
        if (LastValue(values, "--threshold") is not { } text)
            return null;
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var threshold)
            && double.IsFinite(threshold) && threshold > 0)
            return threshold;
        throw new OptionsException($"--threshold expects a finite number above 0, got '{text}'");
    }
}
