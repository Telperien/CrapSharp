using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CrapSharp.Complexity;

/// <summary>Computes the cyclomatic complexity of every member that has a body in a C# source file.</summary>
public static class ComplexityCalculator
{
    /// <summary>Name of the entry for the top-level statements of a file (a Program.cs without a class).</summary>
    public const string TopLevelStatementsName = "<top-level statements>";

    public static IReadOnlyList<MethodComplexity> Analyze(string source, string filePath)
    {
        var root = CSharpSyntaxTree.ParseText(source, path: filePath).GetCompilationUnitRoot();
        var members = root.DescendantNodes()
            .Where(node => HasBody(node) && !CoverageExclusion.IsExcluded(node))
            .Select(member => ToResult(member, filePath));
        return TopLevelStatements(root, filePath).Concat(members).ToList();
    }

    // Abstract, extern and auto-property members have no body: nothing to measure.
    private static bool HasBody(SyntaxNode node) => node switch
    {
        BaseMethodDeclarationSyntax m => m.Body is not null || m.ExpressionBody is not null,
        AccessorDeclarationSyntax a => a.Body is not null || a.ExpressionBody is not null,
        ArrowExpressionClauseSyntax { Parent: BasePropertyDeclarationSyntax } => true,
        _ => false,
    };

    private static MethodComplexity ToResult(SyntaxNode member, string filePath)
    {
        var span = member.GetLocation().GetLineSpan();
        return new MethodComplexity(
            MemberNaming.FullNameOf(member),
            filePath,
            span.StartLinePosition.Line + 1,
            span.EndLinePosition.Line + 1,
            1 + DecisionCounter.CountIn(member));
    }

    // The compiler turns all the top-level statements of a file into one Main method, so they make one entry.
    // They always come before the types of the file, so their line range holds nothing else.
    private static MethodComplexity[] TopLevelStatements(CompilationUnitSyntax root, string filePath)
    {
        var statements = root.Members.OfType<GlobalStatementSyntax>().ToList();
        if (statements.Count == 0)
            return [];
        return
        [
            new MethodComplexity(
                TopLevelStatementsName,
                filePath,
                statements[0].GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                statements[^1].GetLocation().GetLineSpan().EndLinePosition.Line + 1,
                1 + statements.Sum(DecisionCounter.CountIn)),
        ];
    }
}
