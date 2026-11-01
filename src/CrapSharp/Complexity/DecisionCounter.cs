using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CrapSharp.Complexity;

/// <summary>Counts the decision points below a syntax node. Lambdas and local functions are counted too.</summary>
public sealed class DecisionCounter : CSharpSyntaxWalker
{
    private static readonly HashSet<SyntaxKind> DecisionKinds =
    [
        SyntaxKind.IfStatement,
        SyntaxKind.WhileStatement,
        SyntaxKind.DoStatement,
        SyntaxKind.ForStatement,
        SyntaxKind.ForEachStatement,
        SyntaxKind.ForEachVariableStatement,
        SyntaxKind.CatchClause,
        SyntaxKind.ConditionalExpression,
        SyntaxKind.CaseSwitchLabel,
        SyntaxKind.CasePatternSwitchLabel,
        SyntaxKind.WhenClause,
        SyntaxKind.LogicalAndExpression,
        SyntaxKind.LogicalOrExpression,
        SyntaxKind.CoalesceExpression,
        SyntaxKind.CoalesceAssignmentExpression,
        SyntaxKind.ConditionalAccessExpression,
        SyntaxKind.AndPattern,
        SyntaxKind.OrPattern,
    ];

    public int Count { get; private set; }

    public static int CountIn(SyntaxNode node)
    {
        var counter = new DecisionCounter();
        counter.Visit(node);
        return counter.Count;
    }

    public override void Visit(SyntaxNode? node)
    {
        if (node is not null && IsDecisionPoint(node))
            Count++;
        base.Visit(node);
    }

    private static bool IsDecisionPoint(SyntaxNode node) =>
        DecisionKinds.Contains(node.Kind()) || IsCountedSwitchArm(node);

    // The final `_ => ...` arm is the "default" and adds no path.
    private static bool IsCountedSwitchArm(SyntaxNode node) =>
        node is SwitchExpressionArmSyntax arm
        && !(arm.Pattern is DiscardPatternSyntax && arm.WhenClause is null);
}
