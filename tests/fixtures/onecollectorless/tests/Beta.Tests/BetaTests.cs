using Lib;
using Xunit;

namespace Lib.Tests;

// Only this project exercises Beta, but it has no coverlet.collector: nothing records that coverage.
public class BetaTests
{
    [Fact]
    public void PickHandlesBothBranches()
    {
        Assert.Equal(1, Beta.Pick(true));
        Assert.Equal(0, Beta.Pick(false));
    }
}
