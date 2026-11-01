using Lib;
using Xunit;

namespace Lib.Tests;

// Each target framework exercises a different method, so no single report holds the whole coverage.
public class CalcTests
{
#if NET9_0
    [Fact]
    public void AbsHandlesBothSigns()
    {
        Assert.Equal(1, Calc.Abs(-1));
        Assert.Equal(1, Calc.Abs(1));
    }
#else
    [Fact]
    public void TwiceHandlesBothBranches()
    {
        Assert.Equal(2, Calc.Twice(1));
        Assert.Equal(int.MaxValue, Calc.Twice(int.MaxValue));
    }
#endif
}
