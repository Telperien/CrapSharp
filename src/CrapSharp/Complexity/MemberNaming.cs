using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CrapSharp.Complexity;

/// <summary>
/// Builds the qualified name of a member. Names follow the IL conventions
/// (.ctor, get_X) so they can later be matched against coverage reports.
/// </summary>
public static class MemberNaming
{
    public static string FullNameOf(SyntaxNode member) =>
        string.Join('.', ContainerNames(member).Append(MemberName(member)));

    private static IEnumerable<string> ContainerNames(SyntaxNode member) =>
        member.Ancestors().Reverse().Select(ContainerName).OfType<string>();

    private static string? ContainerName(SyntaxNode ancestor) => ancestor switch
    {
        BaseNamespaceDeclarationSyntax ns => ns.Name.ToString(),
        BaseTypeDeclarationSyntax type => type.Identifier.Text,
        _ => null,
    };

    private static string MemberName(SyntaxNode member) => member switch
    {
        MethodDeclarationSyntax m => m.Identifier.Text + ParameterList(m.ParameterList),
        ConstructorDeclarationSyntax c => (c.Modifiers.Any(SyntaxKind.StaticKeyword) ? ".cctor" : ".ctor") + ParameterList(c.ParameterList),
        DestructorDeclarationSyntax => "Finalize",
        OperatorDeclarationSyntax o => "operator " + o.OperatorToken.Text + ParameterList(o.ParameterList),
        ConversionOperatorDeclarationSyntax c => "operator " + c.Type + ParameterList(c.ParameterList),
        AccessorDeclarationSyntax a => a.Keyword.Text + "_" + AccessorOwnerName(a),
        ArrowExpressionClauseSyntax { Parent: BasePropertyDeclarationSyntax p } => "get_" + PropertyName(p),
        _ => member.Kind().ToString(),
    };

    private static string AccessorOwnerName(AccessorDeclarationSyntax accessor) =>
        accessor.Parent?.Parent is BasePropertyDeclarationSyntax owner ? PropertyName(owner) : "?";

    private static string PropertyName(BasePropertyDeclarationSyntax property) => property switch
    {
        PropertyDeclarationSyntax p => p.Identifier.Text,
        EventDeclarationSyntax e => e.Identifier.Text,
        _ => "Item", // indexer
    };

    private static string ParameterList(ParameterListSyntax list) =>
        "(" + string.Join(", ", list.Parameters.Select(p => p.Type?.ToString() ?? "?")) + ")";
}
