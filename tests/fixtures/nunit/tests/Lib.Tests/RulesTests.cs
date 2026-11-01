using NUnit.Framework;

namespace Lib.Tests;

public class RulesTests
{
    [Test]
    public void SignHandlesBothBranches()
    {
        Assert.That(Rules.Sign(5), Is.EqualTo(1));
        Assert.That(Rules.Sign(-5), Is.EqualTo(0));
    }
}
