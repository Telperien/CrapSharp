using System.Xml.Linq;
using CrapSharp.Coverage;
using Xunit;

namespace CrapSharp.Tests;

public class CoverageTests
{
    private static readonly string File1 = Path.GetFullPath("Foo.cs");

    private static CoverageMap MapWith(params (int line, int hits)[] lines)
    {
        var map = new CoverageMap();
        foreach (var (line, hits) in lines)
            map.Record(File1, line, hits);
        return map;
    }

    [Fact]
    public void CoverageIsTheShareOfHitLinesInTheRange() =>
        Assert.Equal(0.5, MapWith((1, 1), (2, 0), (3, 4), (4, 0), (10, 9)).CoverageOf(File1, 1, 4));

    [Fact]
    public void LinesOutsideTheRangeAreIgnored() =>
        Assert.Equal(1.0, MapWith((1, 0), (5, 2), (9, 0)).CoverageOf(File1, 5, 5));

    [Fact]
    public void UnknownFileOrEmptyRangeCountsAsZero()
    {
        var map = MapWith((1, 1));
        Assert.Equal(0, map.CoverageOf(Path.GetFullPath("Other.cs"), 1, 1));
        Assert.Equal(0, map.CoverageOf(File1, 50, 60));
    }

    [Fact]
    public void RecordingTheSameLineKeepsTheHighestHits()
    {
        var map = MapWith((1, 0), (1, 3), (1, 0));
        Assert.Equal(1.0, map.CoverageOf(File1, 1, 1));
    }

    [Fact]
    public void MergeCombinesReports()
    {
        var first = MapWith((1, 1), (2, 0));
        first.Merge(MapWith((2, 1)));
        Assert.Equal(1.0, first.CoverageOf(File1, 1, 2));
    }

    [Fact]
    public void CoberturaLinesAreReadFromClassesAndMethodsWithoutDoubleCounting()
    {
        var xml = $"""
            <coverage><packages><package><classes>
              <class filename="{File1}">
                <methods><method name="M"><lines>
                  <line number="1" hits="2"/><line number="2" hits="0"/>
                </lines></method></methods>
                <lines><line number="1" hits="2"/><line number="2" hits="0"/></lines>
              </class>
            </classes></package></packages></coverage>
            """;
        var map = CoberturaParser.Parse(XDocument.Parse(xml));
        Assert.Equal(1, map.FileCount);
        Assert.Equal(0.5, map.CoverageOf(File1, 1, 2));
    }

    [Fact]
    public void RelativeFilenamesAreResolvedAgainstTheSources()
    {
        var source = Path.GetTempPath();
        var xml = $"""
            <coverage><sources><source>{source}</source></sources><packages><package><classes>
              <class filename="Rel.cs"><lines><line number="1" hits="1"/></lines></class>
            </classes></package></packages></coverage>
            """;
        var map = CoberturaParser.Parse(XDocument.Parse(xml));
        Assert.Equal(1.0, map.CoverageOf(Path.Combine(source, "Rel.cs"), 1, 1));
    }
}
