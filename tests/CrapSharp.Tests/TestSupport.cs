using CrapSharp.Processes;

namespace CrapSharp.Tests;

/// <summary>Records every launched process and answers through a handler instead of starting anything.</summary>
public sealed class FakeProcessRunner(Func<string, IReadOnlyList<string>, string, ProcessResult>? handler = null) : IProcessRunner
{
    public List<(string File, string[] Arguments, string WorkingDirectory)> Calls { get; } = [];

    public ProcessResult Run(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
    {
        Calls.Add((fileName, arguments.ToArray(), workingDirectory));
        return handler?.Invoke(fileName, arguments, workingDirectory) ?? new ProcessResult(0, "", "");
    }
}

/// <summary>A scratch folder with helpers to lay out a small solution on disk.</summary>
public sealed class TempWorkspace : IDisposable
{
    public string Root { get; } = Directory.CreateTempSubdirectory("crapsharp-ws").FullName;

    public void Dispose() => Directory.Delete(Root, recursive: true);

    public string Write(string relativePath, string content)
    {
        var full = Path.GetFullPath(Path.Combine(Root, relativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return full;
    }

    public string Path_(string relativePath) => Path.GetFullPath(Path.Combine(Root, relativePath));

    /// <summary>Layout: src/App (one method, CC 2) and tests/App.Tests (a test project).</summary>
    public (string Source, string TestProject) CreateSolution()
    {
        Write("src/App/App.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        var source = Write("src/App/Calc.cs", "namespace App;\nclass Calc\n{\n    int Sign(int x)\n    {\n        if (x > 0) return 1;\n        return 0;\n    }\n}\n");
        Write("src/App/obj/Generated.cs", "class G { void M() { } }");
        var testProject = Write("tests/App.Tests/App.Tests.csproj", "<Project><ItemGroup><PackageReference Include=\"Microsoft.NET.Test.Sdk\" /></ItemGroup></Project>");
        Write("tests/App.Tests/CalcTests.cs", "class CalcTests { void T() { } }");
        return (source, testProject);
    }

    /// <summary>Writes a minimal Cobertura report for one file: lines 1..n with the given hits.</summary>
    public static string CoberturaXml(string sourceFile, params (int line, int hits)[] lines) =>
        "<coverage><packages><package><classes>" +
        $"<class filename=\"{sourceFile}\"><lines>" +
        string.Concat(lines.Select(l => $"<line number=\"{l.line}\" hits=\"{l.hits}\"/>")) +
        "</lines></class></classes></package></packages></coverage>";
}
