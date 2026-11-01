using Xunit;

namespace Lib.Tests;

public class ClosuresTests
{
    // strict is false: the clamp lambda is created but never called.
    [Fact]
    public void ScoreSumsTheWeights() => Assert.Equal(3, Closures.Score([1, 20], strict: false));

    // fail is false: the early return is never taken.
    [Fact]
    public async Task LoadAsyncSucceeds() => Assert.Equal(1, await Closures.LoadAsync(fail: false));

    [Fact]
    public void EvensYieldsTheEvenNumbers() => Assert.Equal([0, 2], Closures.Evens(4).ToList());
}
