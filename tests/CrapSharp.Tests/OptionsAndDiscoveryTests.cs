using CrapSharp;
using CrapSharp.Complexity;
using Xunit;

namespace CrapSharp.Tests;

public class OptionsAndDiscoveryTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("crapsharp-tests").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string Touch(string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(_dir, relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, "class X { }");
        return full;
    }

    [Fact]
    public void ParseDefaultsToCurrentDirectoryWithoutCoverage()
    {
        var options = Options.Parse([]);
        Assert.Equal(".", options.Path);
        Assert.Empty(options.CoverageReports);
    }

    [Fact]
    public void ParseReadsThePathAndEveryCoverageReport()
    {
        var options = Options.Parse(["src", "--coverage", "a.xml", "--coverage", "b.xml"]);
        Assert.Equal("src", options.Path);
        Assert.Equal(["a.xml", "b.xml"], options.CoverageReports);
    }

    [Fact]
    public void ParseReadsFlagsAndValuedOptions()
    {
        var options = Options.Parse(
            ["--changed", "--use-existing-coverage", "--filter", "Orders", "--coverage-command", "make cover", "--no-coverage"]);
        Assert.True(options.Changed);
        Assert.True(options.UseExistingCoverage);
        Assert.True(options.NoCoverage);
        Assert.Equal("Orders", options.Filter);
        Assert.Equal("make cover", options.CoverageCommand);
    }

    [Theory]
    [InlineData(new string[0], CoverageMode.Run)]
    [InlineData(new[] { "--use-existing-coverage" }, CoverageMode.UseExisting)]
    [InlineData(new[] { "--coverage-command", "c", "--use-existing-coverage" }, CoverageMode.Command)]
    [InlineData(new[] { "--coverage", "r.xml", "--coverage-command", "c" }, CoverageMode.Explicit)]
    [InlineData(new[] { "--coverage", "r.xml", "--no-coverage" }, CoverageMode.None)]
    public void CoverageModeFollowsTheOptionPriority(string[] args, CoverageMode expected) =>
        Assert.Equal(expected, Options.Parse(args).Mode);

    [Fact]
    public void AValuedOptionWithoutValueIsRejected()
    {
        var e = Assert.Throws<OptionsException>(() => Options.Parse(["src", "--filter"]));
        Assert.Equal("--filter expects a value", e.Message);
    }

    [Fact]
    public void AnUnknownOptionIsRejectedInsteadOfIgnored()
    {
        var e = Assert.Throws<OptionsException>(() => Options.Parse(["--treshold", "10"]));
        Assert.Equal("Unknown option: --treshold", e.Message);
    }

    [Fact]
    public void ASecondPathIsRejected() =>
        Assert.Throws<OptionsException>(() => Options.Parse(["src", "tests"]));

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public void ParseRecognizesHelp(string flag) => Assert.True(Options.Parse([flag]).Help);

    [Fact]
    public void DiscoverySkipsBuildOutputAndGeneratedCode()
    {
        var kept = Touch("src/Foo.cs");
        Touch("bin/Debug/A.cs");
        Touch("src/obj/B.cs");
        Touch("src/View.g.cs");
        Touch("src/Form.Designer.cs");

        Assert.Equal([kept], SourceDiscovery.FindCSharpFiles(_dir));
    }

    [Fact]
    public void DiscoveryAcceptsASingleFile()
    {
        var file = Touch("Only.cs");
        Assert.Equal([file], SourceDiscovery.FindCSharpFiles(file));
    }

    [Fact]
    public void DestructorsOperatorsIndexersAndEventsAreNamed()
    {
        var source = """
            class C
            {
                ~C() { }
                public static C operator +(C a, C b) { return a; }
                public static implicit operator int(C c) { return 1; }
                int this[int i] { get { return i; } }
                event System.Action E { add { } remove { } }
            }
            """;
        var names = ComplexityCalculator.Analyze(source, "Test.cs").Select(m => m.FullName);
        Assert.Equal(
            ["C.Finalize", "C.operator +(C, C)", "C.operator int(C)", "C.get_Item", "C.add_E", "C.remove_E"],
            names);
    }
}
