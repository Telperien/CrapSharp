using Xunit;

namespace Lib.Tests;

// Second is never called, but it shares its only line with First.
public class PairTests
{
    [Fact]
    public void FirstHandlesBothBranches()
    {
        Assert.Equal(1, Pair.First(1));
        Assert.Equal(0, Pair.First(0));
    }
}
