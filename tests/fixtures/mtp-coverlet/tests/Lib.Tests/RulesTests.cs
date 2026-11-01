namespace Lib.Tests;

[TestClass]
public class RulesTests
{
    [TestMethod]
    public void SignHandlesBothBranches()
    {
        Assert.AreEqual(1, Rules.Sign(5));
        Assert.AreEqual(0, Rules.Sign(-5));
    }
}
