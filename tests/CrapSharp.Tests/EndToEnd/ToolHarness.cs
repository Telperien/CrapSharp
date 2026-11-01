using System.Text.Json;
using CrapSharp.Processes;

namespace CrapSharp.Tests.EndToEnd;

/// <summary>The tool as users get it: `dotnet publish` output, published once for all end-to-end tests.</summary>
public static class PublishedTool
{
    private static readonly Lazy<string> Dll = new(Publish);

    public static string RepositoryRoot { get; } = FindRepositoryRoot();

    public static ToolRun Run(string workingDirectory, params string[] args)
    {
        var result = new SystemProcessRunner().Run("dotnet", [Dll.Value, .. args], workingDirectory);
        return new ToolRun(result.ExitCode, result.Output, result.Error, workingDirectory);
    }

    private static string Publish()
    {
        var output = Path.Combine(Path.GetTempPath(), $"crapsharp-e2e-tool-{Guid.NewGuid():N}");
        var result = new SystemProcessRunner().Run(
            "dotnet", ["publish", Path.Combine(RepositoryRoot, "src", "CrapSharp"), "-c", "Release", "-o", output], RepositoryRoot);
        return result.ExitCode == 0 ? Path.Combine(output, "CrapSharp.dll") : throw new InvalidOperationException(result.Combined);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CrapSharp.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("CrapSharp.sln not found above the test binaries");
    }
}

/// <summary>One run of the published tool. Root is the folder it analyzed.</summary>
public sealed record ToolRun(int ExitCode, string Output, string Error, string Root)
{
    private string JsonPath => Path.Combine(Root, ".metrics", "crap.json");

    public bool HasJson => File.Exists(JsonPath);

    public JsonDocument Json() => JsonDocument.Parse(File.ReadAllText(JsonPath));

    /// <summary>The JSON entry of the method whose name ends with the given text.</summary>
    public Entry EntryOf(string methodName)
    {
        using var json = Json();
        var matches = json.RootElement.GetProperty("entries").EnumerateArray()
            .Where(e => e.GetProperty("name").GetString()!.EndsWith(methodName, StringComparison.Ordinal))
            .Select(e => new Entry(
                e.GetProperty("name").GetString()!,
                e.GetProperty("complexity").GetInt32(),
                e.GetProperty("coverage").GetDouble(),
                e.GetProperty("crapRaw").GetDouble()))
            .ToList();
        return matches.Count == 1 ? matches[0] : throw new InvalidOperationException($"{matches.Count} entries match {methodName}");
    }

    public IReadOnlyList<string> Names()
    {
        using var json = Json();
        return json.RootElement.GetProperty("entries").EnumerateArray().Select(e => e.GetProperty("name").GetString()!).ToList();
    }

    /// <summary>The Cobertura reports the JSON says the scores were computed from.</summary>
    public IReadOnlyList<string> CoverageReports()
    {
        using var json = Json();
        return json.RootElement.GetProperty("coverage").GetProperty("reports").EnumerateArray().Select(r => r.GetString()!).ToList();
    }

    /// <summary>Every Cobertura report the tool's own `dotnet test` runs left, even when no JSON was written.</summary>
    public IReadOnlyList<string> CoverageReportsOnDisk() =>
        Directory.EnumerateFiles(Path.Combine(Root, ".metrics", "coverage"), "*.xml", SearchOption.AllDirectories).ToList();
}

/// <param name="Coverage">Percent, 0 to 100, truncated to one decimal.</param>
/// <param name="CrapRaw">The exact score the threshold verdict compares.</param>
public sealed record Entry(string Name, int Complexity, double Coverage, double CrapRaw);

/// <summary>A copy of a fixture in a scratch folder, so builds and test results never touch the repository.</summary>
public sealed class FixtureCopy : IDisposable
{
    private const string ProjectTemplateSuffix = ".csproj.template";

    private FixtureCopy(string name, string root) => (Name, Root) = (name, root);

    public string Name { get; }

    public string Root { get; }

    /// <summary>
    /// Fixture projects are stored as *.csproj.template so that running the tool on this repository never
    /// finds them (their tests would run, and some fail on purpose); the copy gets real .csproj files.
    /// </summary>
    public static FixtureCopy Of(string name)
    {
        var source = Path.Combine(PublishedTool.RepositoryRoot, "tests", "fixtures", name);
        var root = Directory.CreateTempSubdirectory($"crapsharp-e2e-{name}-").FullName;
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            if (relative.EndsWith(ProjectTemplateSuffix, StringComparison.Ordinal))
                relative = relative[..^".template".Length];
            var target = Path.Combine(root, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
        return new FixtureCopy(name, root);
    }

    public ToolRun Run(params string[] args) => PublishedTool.Run(Root, [Root, .. args]);

    /// <summary>The coverage reports and the JSON are kept under artifacts/e2e/ (not tracked) for diagnosis.</summary>
    public void Dispose()
    {
        KeepDiagnostics();
        try
        {
            // Git writes its objects read-only, which Directory.Delete refuses on Windows.
            foreach (var file in Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // A build server may still hold a file; the scratch folder is disposable.
        }
    }

    private void KeepDiagnostics()
    {
        var metrics = Path.Combine(Root, ".metrics");
        if (!Directory.Exists(metrics))
            return;
        var target = Path.Combine(PublishedTool.RepositoryRoot, "artifacts", "e2e", Name, ".metrics");
        if (Directory.Exists(target))
            Directory.Delete(target, recursive: true);
        foreach (var file in Directory.EnumerateFiles(metrics, "*", SearchOption.AllDirectories))
        {
            var copy = Path.Combine(target, Path.GetRelativePath(metrics, file));
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            File.Copy(file, copy, overwrite: true);
        }
    }
}
