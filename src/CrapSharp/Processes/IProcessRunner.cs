namespace CrapSharp.Processes;

/// <summary>Output and error are kept apart because git's stdout is parsed while stderr may carry warnings.</summary>
public sealed record ProcessResult(int ExitCode, string Output, string Error)
{
    public string Combined => Output + Error;
}

/// <summary>Seam over process launching, so the tool can be tested without running dotnet or git.</summary>
public interface IProcessRunner
{
    ProcessResult Run(string fileName, IReadOnlyList<string> arguments, string workingDirectory);
}
