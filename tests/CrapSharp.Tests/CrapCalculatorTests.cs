using CrapSharp.Scoring;
using Xunit;

namespace CrapSharp.Tests;

public class CrapCalculatorTests
{
    [Theory]
    [InlineData(3, 0.75, 3.140625)]
    [InlineData(1, 1.0, 1.0)]
    [InlineData(10, 1.0, 10.0)]
    [InlineData(10, 0.0, 110.0)]
    [InlineData(4, 0.5, 6.0)]
    public void ScoreFollowsTheCrapFormula(int complexity, double coverage, double expected) =>
        Assert.Equal(expected, CrapCalculator.Score(complexity, coverage), 6);

    [Fact]
    public void CoverageOutsideZeroToOneIsClamped()
    {
        Assert.Equal(CrapCalculator.Score(5, 1.0), CrapCalculator.Score(5, 1.7));
        Assert.Equal(CrapCalculator.Score(5, 0.0), CrapCalculator.Score(5, -0.3));
    }
}
