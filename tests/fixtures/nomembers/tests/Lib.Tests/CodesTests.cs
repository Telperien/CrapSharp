using Xunit;

namespace Lib.Tests;

public class CodesTests
{
    [Fact]
    public void OkIsZero() => Assert.Equal(0, Codes.Ok);
}
