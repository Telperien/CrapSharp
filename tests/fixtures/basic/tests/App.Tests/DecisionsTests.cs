using App;
using Xunit;

namespace App.Tests;

public class DecisionsTests
{
    // 100 passes every test of Ten, so every line of it runs.
    [Fact]
    public void TenCountsTheThresholdsPassed() => Assert.Equal(9, Decisions.Ten(100));
}
