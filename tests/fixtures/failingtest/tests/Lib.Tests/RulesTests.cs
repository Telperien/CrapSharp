using Xunit;

namespace Lib.Tests;

public class RulesTests
{
    [Fact]
    public void PositiveIsOne() => Assert.Equal(1, Rules.Sign(5));

    // Fails on purpose (the real answer is -1) after running the negative branch.
    [Fact]
    public void NegativeIsWronglyExpectedToBeZero() => Assert.Equal(0, Rules.Sign(-5));
}
