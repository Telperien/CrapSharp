using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace CrapSharp.Processes;

public sealed class SystemProcessRunner : IProcessRunner
{
    /// <summary>Exit code reported when the executable cannot be started (same as a shell's "command not found").</summary>
    public const int NotFound = 127;

    public ProcessResult Run(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
    {
        var info = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);
        try
        {
            return Execute(info);
        }
        catch (Win32Exception e)
        {
            return new ProcessResult(NotFound, "", e.Message);
        }
    }

    private static ProcessResult Execute(ProcessStartInfo info)
    {
        using var process = new Process { StartInfo = info };
        var output = new StringBuilder();
        var error = new StringBuilder();
        process.OutputDataReceived += (_, e) => Append(output, e.Data);
        process.ErrorDataReceived += (_, e) => Append(error, e.Data);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        return new ProcessResult(process.ExitCode, output.ToString(), error.ToString());
    }

    private static void Append(StringBuilder builder, string? line)
    {
        if (line is null)
            return;
        lock (builder)
            builder.AppendLine(line);
    }
}
