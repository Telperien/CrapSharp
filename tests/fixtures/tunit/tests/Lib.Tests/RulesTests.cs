namespace Lib.Tests;

public class RulesTests
{
    [Test]
    public async Task SignHandlesBothBranches()
    {
        await Assert.That(Rules.Sign(5)).IsEqualTo(1);
        await Assert.That(Rules.Sign(-5)).IsEqualTo(0);
    }
}
