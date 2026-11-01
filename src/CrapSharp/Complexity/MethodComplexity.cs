namespace CrapSharp.Complexity;

/// <summary>Cyclomatic complexity of one callable member (method, constructor, accessor...).</summary>
/// <param name="FullName">Namespace.Type.Member, with the parameter types for methods, e.g. "Demo.Calc.Add(int, int)".</param>
/// <param name="FilePath">Path of the source file, as given to the analyzer.</param>
/// <param name="StartLine">1-based first line of the member.</param>
/// <param name="EndLine">1-based last line of the member.</param>
/// <param name="Complexity">1 + number of decision points.</param>
public sealed record MethodComplexity(
    string FullName,
    string FilePath,
    int StartLine,
    int EndLine,
    int Complexity);
