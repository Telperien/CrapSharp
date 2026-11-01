using CrapSharp.Processes;
using Xunit;

namespace CrapSharp.Tests.EndToEnd;

/// <summary>The tool as a user installs it: packed, then restored from a local source into a clean tool manifest.</summary>
[Trait("Category", "EndToEnd")]
public class PackagingTests
{
    [EndToEndFact]
    public void ThePackedToolInstallsFromACleanManifestAndGatesAFixture()
    {
        var runner = new SystemProcessRunner();
        // A unique version keeps the NuGet cache of an earlier build from being reused.
        var version = $"0.0.0-e2e.{DateTime.UtcNow.Ticks}";
        var packages = Directory.CreateTempSubdirectory("crapsharp-e2e-nupkg-").FullName;
        var clean = Directory.CreateTempSubdirectory("crapsharp-e2e-clean-").FullName;
        try
        {
            Run(runner, PublishedTool.RepositoryRoot, "pack", Path.Combine(PublishedTool.RepositoryRoot, "src", "CrapSharp"),
                "-c", "Release", "-o", packages, $"-p:Version={version}");
            Run(runner, clean, "new", "tool-manifest");
            Run(runner, clean, "tool", "install", "CrapSharp", "--version", version, "--add-source", packages);

            var help = runner.Run("dotnet", ["crapsharp", "--help"], clean);
            Assert.Equal(0, help.ExitCode);
            Assert.Contains("Usage: crapsharp", help.Output);

            using var fixture = FixtureCopy.Of("basic");
            var gate = runner.Run("dotnet", ["crapsharp", fixture.Root, "--threshold", "10"], clean);
            // Ten scores exactly 10: the installed tool must fail the strict gate, as the published build does.
            Assert.Equal(2, gate.ExitCode);
            Assert.Contains("1 method(s) score 10 or more", gate.Error);
        }
        finally
        {
            Directory.Delete(packages, recursive: true);
            Directory.Delete(clean, recursive: true);
        }
    }

    private static void Run(SystemProcessRunner runner, string directory, params string[] args)
    {
        var result = runner.Run("dotnet", args, directory);
        Assert.True(result.ExitCode == 0, result.Combined);
    }
}
