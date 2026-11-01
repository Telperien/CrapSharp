using CrapSharp.Complexity;
using Xunit;

namespace CrapSharp.Tests;

public class ComplexityCalculatorTests
{
    private static MethodComplexity Single(string source)
    {
        var results = ComplexityCalculator.Analyze(source, "Test.cs");
        return Assert.Single(results);
    }

    private static int Cc(string methodBody) =>
        Single($"class C {{ int M(int x, int y, string s) {{ {methodBody} }} }}").Complexity;

    [Fact]
    public void EmptyMethodHasComplexityOne() => Assert.Equal(1, Cc("return 0;"));

    [Theory]
    [InlineData("if (x > 0) return 1; return 0;", 2)]
    [InlineData("if (x > 0) return 1; else if (x < 0) return 2; return 0;", 3)]
    [InlineData("for (var i = 0; i < x; i++) { } return 0;", 2)]
    [InlineData("foreach (var c in s) { } return 0;", 2)]
    [InlineData("while (x > 0) x--; return 0;", 2)]
    [InlineData("do { x--; } while (x > 0); return 0;", 2)]
    [InlineData("try { return 1; } catch (Exception) { return 2; }", 2)]
    [InlineData("try { return 1; } catch (ArgumentException) { return 2; } catch (Exception) { return 3; }", 3)]
    public void StatementsAddOnePathEach(string body, int expected) => Assert.Equal(expected, Cc(body));

    [Theory]
    [InlineData("return x > 0 ? 1 : 2;", 2)]
    [InlineData("return x > 0 && y > 0 ? 1 : 2;", 3)]
    [InlineData("if (x > 0 || y > 0) return 1; return 0;", 3)]
    [InlineData("return s ?? \"a\";", 2)]
    [InlineData("return s ?? s ?? \"a\";", 3)]
    [InlineData("return s?.Length ?? 0;", 3)]
    [InlineData("return s?.Trim()?.Length ?? 0;", 4)]
    [InlineData("s ??= \"a\"; return 0;", 2)]
    [InlineData("return s is \"a\" or \"b\" ? 1 : 0;", 3)]
    public void OperatorsAddOnePathEach(string body, int expected) => Assert.Equal(expected, Cc(body));

    [Fact]
    public void SwitchStatementCountsEachCaseButNotDefault() =>
        Assert.Equal(4, Cc("switch (x) { case 1: return 1; case 2: return 2; case 3: return 3; default: return 0; }"));

    [Fact]
    public void SwitchCaseGuardAddsAPath() =>
        Assert.Equal(3, Cc("switch (x) { case int n when n > 0: return 1; default: return 0; }"));

    [Fact]
    public void SwitchExpressionCountsEachArmButNotTheDiscard() =>
        Assert.Equal(3, Cc("return x switch { 1 => 10, 2 => 20, _ => 0 };"));

    [Fact]
    public void LambdaAndLocalFunctionCountTowardTheEnclosingMember()
    {
        Assert.Equal(2, Cc("Func<int, int> f = a => a > 0 ? 1 : 2; return f(x);"));
        Assert.Equal(2, Cc("int Local(int a) { if (a > 0) return 1; return 0; } return Local(x);"));
    }

    [Fact]
    public void MembersWithoutBodyAreSkipped()
    {
        var source = "abstract class C { public abstract void A(); public int P { get; set; } }";
        Assert.Empty(ComplexityCalculator.Analyze(source, "Test.cs"));
    }

    [Fact]
    public void NamesIncludeNamespaceNestedTypesAndParameterTypes()
    {
        var source = "namespace Demo.App; class Outer { class Inner { int Add(int a, string b) => a; } }";
        Assert.Equal("Demo.App.Outer.Inner.Add(int, string)", Single(source).FullName);
    }

    [Fact]
    public void BlockScopedNamespaceIsSupported()
    {
        var source = "namespace Demo { class C { void M() { } } }";
        Assert.Equal("Demo.C.M()", Single(source).FullName);
    }

    [Fact]
    public void ConstructorsAndStaticConstructorsUseIlNames()
    {
        var source = "class C { static C() { } public C(int a) { } }";
        var names = ComplexityCalculator.Analyze(source, "Test.cs").Select(m => m.FullName);
        Assert.Equal(["C..cctor()", "C..ctor(int)"], names);
    }

    [Fact]
    public void AccessorsAndExpressionBodiedPropertiesUseIlNames()
    {
        var source = "class C { int f; int A { get { return f; } set { f = value; } } int B => f > 0 ? 1 : 2; }";
        var results = ComplexityCalculator.Analyze(source, "Test.cs");
        Assert.Equal(["C.get_A", "C.set_A", "C.get_B"], results.Select(m => m.FullName));
        Assert.Equal([1, 1, 2], results.Select(m => m.Complexity));
    }

    [Fact]
    public void LineRangeIsOneBased()
    {
        var source = "class C\n{\n    void M()\n    {\n    }\n}\n";
        var result = Single(source);
        Assert.Equal(3, result.StartLine);
        Assert.Equal(5, result.EndLine);
    }

    [Theory]
    [InlineData("[ExcludeFromCodeCoverage] void Skipped() { }")]
    [InlineData("[ExcludeFromCodeCoverageAttribute] void Skipped() { }")]
    [InlineData("[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] void Skipped() { }")]
    [InlineData("[Obsolete, ExcludeFromCoverage] void Skipped() { }")]
    [InlineData("[ExcludeFromCodeCoverage] int Skipped { get { return 1; } }")]
    [InlineData("int Skipped { [ExcludeFromCodeCoverage] get { return 1; } }")]
    public void MembersCoverletExcludesAreNotScored(string excluded)
    {
        var results = ComplexityCalculator.Analyze($"class C {{ {excluded} void Kept() {{ }} }}", "Test.cs");
        Assert.Equal(["C.Kept()"], results.Select(m => m.FullName));
    }

    [Fact]
    public void AnExcludedTypeExcludesItsMembersAndNestedTypes()
    {
        var source = "[ExcludeFromCodeCoverage] class C { void M() { } class Inner { void N() { } } } class D { void K() { } }";
        Assert.Equal(["D.K()"], ComplexityCalculator.Analyze(source, "Test.cs").Select(m => m.FullName));
    }

    [Fact]
    public void TopLevelStatementsMakeOneEntry()
    {
        var source = "using System;\n\nvar x = args.Length;\nif (x > 0)\n    Console.WriteLine(x > 1 ? \"many\" : \"one\");\nreturn 0;\n\nclass Helper { void M() { } }\n";
        var results = ComplexityCalculator.Analyze(source, "Program.cs");
        var main = results[0];
        Assert.Equal(ComplexityCalculator.TopLevelStatementsName, main.FullName);
        Assert.Equal(3, main.StartLine);
        Assert.Equal(6, main.EndLine);
        Assert.Equal(3, main.Complexity);
        Assert.Equal("Helper.M()", results[1].FullName);
    }

    [Fact]
    public void AFileWithoutTopLevelStatementsHasNoSuchEntry() =>
        Assert.DoesNotContain(
            ComplexityCalculator.Analyze("class C { void M() { } }", "Test.cs"),
            m => m.FullName == ComplexityCalculator.TopLevelStatementsName);

    [Fact]
    public void OverloadsAreReportedSeparately()
    {
        var source = "class C { void M(int a) { } void M(string a) { if (a == null) return; } }";
        var results = ComplexityCalculator.Analyze(source, "Test.cs");
        Assert.Equal(["C.M(int)", "C.M(string)"], results.Select(m => m.FullName));
    }
}
