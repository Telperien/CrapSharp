using Xunit;

namespace Lib.Tests;

public class RulesTests
{
    [Fact]
    public void SignHandlesBothBranches()
    {
        Assert.Equal(1, Rules.Sign(5));
        Assert.Equal(0, Rules.Sign(-5));
    }
}
