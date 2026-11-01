namespace CrapSharp.Coverage;

/// <summary>How a test project runs and writes its Cobertura report.</summary>
public enum TestRunner
{
    /// <summary>`dotnet test` with the coverlet.collector data collector.</summary>
    VsTest,
    /// <summary>Microsoft.Testing.Platform with Microsoft.Testing.Extensions.CodeCoverage (--coverage).</summary>
    MtpCodeCoverage,
    /// <summary>Microsoft.Testing.Platform with coverlet.MTP (--coverlet).</summary>
    MtpCoverlet,
    /// <summary>Microsoft.Testing.Platform with no extension able to write a Cobertura report.</summary>
    MtpWithoutCoverage,
}

public static class TestRunners
{
    private static readonly string[] PlatformMarkers =
        ["EnableMSTestRunner", "UseMicrosoftTestingPlatformRunner", "TestingPlatformDotnetTestSupport", "MSTest.Sdk", "xunit.v3", "Include=\"TUnit"];

    /// <summary>Told from the project file text only (no MSBuild evaluation, so no Directory.Build.props).</summary>
    public static TestRunner Of(string projectFileText)
    {
        if (!PlatformMarkers.Any(marker => projectFileText.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            return TestRunner.VsTest;
        if (UsesXunitV3Runner(projectFileText))
            return TestRunner.MtpWithoutCoverage;
        var coverlet = projectFileText.Contains("coverlet.MTP", StringComparison.OrdinalIgnoreCase);
        // TUnit brings the code coverage extension with it; xunit.v3 only runs on the platform when the project opts in.
        if (projectFileText.Contains("Microsoft.Testing.Extensions.CodeCoverage", StringComparison.OrdinalIgnoreCase)
            || (!coverlet && projectFileText.Contains("Include=\"TUnit", StringComparison.OrdinalIgnoreCase)))
            return TestRunner.MtpCodeCoverage;
        return coverlet ? TestRunner.MtpCoverlet : TestRunner.MtpWithoutCoverage;
    }

    /// <summary>xunit.v3 starts its own runner, which has no --coverage, unless the project opts into the platform.</summary>
    public static bool UsesXunitV3Runner(string projectFileText) =>
        projectFileText.Contains("xunit.v3", StringComparison.OrdinalIgnoreCase)
        && !projectFileText.Contains("UseMicrosoftTestingPlatformRunner", StringComparison.OrdinalIgnoreCase)
        && !projectFileText.Contains("xunit.v3.mtp", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// `dotnet run` rather than `dotnet test`: on the .NET 10 SDK, `dotnet test` refuses a Microsoft.Testing.Platform
    /// project unless global.json opts in, while a test project is an executable that `dotnet run` always starts.
    /// </summary>
    public static IReadOnlyList<string> Arguments(TestRunner runner, string project, string resultsDirectory) => runner switch
    {
        TestRunner.MtpCodeCoverage => MtpArguments(project, resultsDirectory, "--coverage", "--coverage-output-format"),
        TestRunner.MtpCoverlet => MtpArguments(project, resultsDirectory, "--coverlet", "--coverlet-output-format"),
        _ => ["test", project, "--collect:XPlat Code Coverage", "--results-directory", resultsDirectory],
    };

    private static string[] MtpArguments(string project, string resultsDirectory, string enable, string format) =>
        ["run", "--project", project, "--", enable, format, "cobertura", "--results-directory", resultsDirectory];
}
