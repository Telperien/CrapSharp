using CrapSharp.Coverage;
using CrapSharp.Projects;
using Xunit;

namespace CrapSharp.Tests;

/// <summary>Which test frameworks make a project a test project, and how each one is run for coverage.</summary>
public class TestFrameworkTests
{
    [Theory]
    [InlineData("<PackageReference Include=\"TUnit\" Version=\"1.78.0\" />")]
    [InlineData("<PackageReference Include=\"TUnit.Core\" Version=\"1.78.0\" />")]
    [InlineData("<PackageReference Include=\"xunit.v3.mtp-v2\" Version=\"4.0.2\" />")]
    public void FrameworkPackagesAloneMakeATestProject(string projectText) =>
        Assert.True(ProjectDiscovery.IsTestProject(projectText));

    [Fact]
    public void AnXunitV3ProjectCannotBeScoredUnlessItOptsIntoMicrosoftTestingPlatform()
    {
        // xunit.v3 starts its own in-process runner, which rejects --coverage; VSTest is refused on the .NET 10 SDK.
        const string native = "<PackageReference Include=\"xunit.v3\" /><PackageReference Include=\"Microsoft.Testing.Extensions.CodeCoverage\" />";
        const string platform = "<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>"
            + "<PackageReference Include=\"xunit.v3\" /><PackageReference Include=\"Microsoft.Testing.Extensions.CodeCoverage\" />";

        Assert.Equal(TestRunner.MtpWithoutCoverage, TestRunners.Of(native));
        Assert.True(TestRunners.UsesXunitV3Runner(native));
        Assert.False(TestRunners.UsesXunitV3Runner(platform));
        Assert.Equal(TestRunner.MtpCodeCoverage, TestRunners.Of(platform));
        Assert.Equal(TestRunner.MtpWithoutCoverage, TestRunners.Of("<PackageReference Include=\"xunit.v3.mtp-v2\" />"));
    }

    [Fact]
    public void TUnitRunsOnTheMicrosoftTestingPlatformWithTheCoverageExtensionItBrings()
    {
        Assert.Equal(TestRunner.MtpCodeCoverage, TestRunners.Of("<PackageReference Include=\"TUnit\" Version=\"1.78.0\" />"));
        Assert.Equal(TestRunner.MtpCoverlet, TestRunners.Of(
            "<PackageReference Include=\"TUnit\" /><PackageReference Include=\"coverlet.MTP\" />"));
    }
}
