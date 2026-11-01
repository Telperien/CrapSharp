using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CrapSharp.Complexity;

/// <summary>
/// Members coverlet leaves out of its report by default: [ExcludeFromCodeCoverage] or [ExcludeFromCoverage] on the
/// member, on its property or event, or on any enclosing type. They are not scored: with no reported line they would
/// wrongly count as 0 % covered.
/// </summary>
public static class CoverageExclusion
{
    private const string Suffix = "Attribute";

    private static readonly HashSet<string> AttributeNames = ["ExcludeFromCodeCoverage", "ExcludeFromCoverage"];

    public static bool IsExcluded(SyntaxNode member) =>
        member.AncestorsAndSelf()
            .SelectMany(AttributeListsOf)
            .SelectMany(list => list.Attributes)
            .Any(IsExclusion);

    private static IEnumerable<AttributeListSyntax> AttributeListsOf(SyntaxNode node) => node switch
    {
        MemberDeclarationSyntax member => member.AttributeLists,
        AccessorDeclarationSyntax accessor => accessor.AttributeLists,
        _ => [],
    };

    private static bool IsExclusion(AttributeSyntax attribute)
    {
        var name = SimpleName(attribute.Name);
        return AttributeNames.Contains(name.EndsWith(Suffix, StringComparison.Ordinal) ? name[..^Suffix.Length] : name);
    }

    // [ExcludeFromCodeCoverage], [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage], [global::...]
    private static string SimpleName(NameSyntax name) => name switch
    {
        QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
        AliasQualifiedNameSyntax alias => alias.Name.Identifier.Text,
        SimpleNameSyntax simple => simple.Identifier.Text,
        _ => name.ToString(),
    };
}
