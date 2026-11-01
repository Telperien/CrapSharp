using Lib;
using Xunit;

namespace Lib.Tests;

// Only this project exercises Alpha: its report alone holds that coverage.
public class AlphaTests
{
    [Fact]
    public void PickHandlesBothBranches()
    {
        Assert.Equal(1, Alpha.Pick(true));
        Assert.Equal(0, Alpha.Pick(false));
    }
}
