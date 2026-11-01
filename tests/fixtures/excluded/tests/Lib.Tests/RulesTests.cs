using Xunit;

namespace Lib.Tests;

public class RulesTests
{
    [Fact]
    public void MeasuredHandlesBothBranches()
    {
        var rules = new Rules();
        Assert.Equal(1, rules.Measured(1));
        Assert.Equal(0, rules.Measured(0));
    }
}
