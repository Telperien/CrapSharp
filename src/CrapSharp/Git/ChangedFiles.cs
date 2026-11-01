using CrapSharp.Processes;

namespace CrapSharp.Git;

/// <summary>The .cs files git sees as modified, added, renamed or untracked.</summary>
public sealed class ChangedFiles(IProcessRunner processes)
{
    /// <summary>Throws InvalidOperationException when git is missing or the folder is not a repository.</summary>
    public HashSet<string> Find(string directory)
    {
        var top = Git(directory, "rev-parse", "--show-toplevel").Trim();
        return Parse(Git(directory, "status", "--porcelain", "-uall"), top);
    }

    /// <summary>
    /// The .cs files changed since a reference, for a pull request: committed after the common ancestor of the reference and HEAD,
    /// modified in the working tree, or untracked. Throws InvalidOperationException when git does not know the reference.
    /// </summary>
    public HashSet<string> FindSince(string directory, string reference)
    {
        var top = Git(directory, "rev-parse", "--show-toplevel").Trim();
        var ancestor = Reference(directory, reference, "merge-base", reference, "HEAD").Trim();
        var changed = Git(directory, "-c", "core.quotepath=false", "diff", "--name-only", "--diff-filter=ACMR", ancestor);
        var untracked = Git(directory, "ls-files", "--others", "--exclude-standard");
        return ParsePaths(changed + "\n" + untracked, top);
    }

    /// <summary>Parses `git status --porcelain`: "XY path" or "XY old -> new", paths relative to the repository root.</summary>
    public static HashSet<string> Parse(string porcelain, string repositoryRoot) =>
        porcelain.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.Length > 3)
            .Select(PathOf)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetFullPath(Path.Combine(repositoryRoot, path)))
            .ToHashSet(PathComparison.Comparer);

    private static HashSet<string> ParsePaths(string lines, string repositoryRoot) =>
        lines.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetFullPath(Path.Combine(repositoryRoot, path)))
            .ToHashSet(PathComparison.Comparer);

    private static string PathOf(string line)
    {
        var path = line[3..];
        var arrow = path.IndexOf(" -> ", StringComparison.Ordinal);
        return (arrow >= 0 ? path[(arrow + 4)..] : path).Trim('"');
    }

    private string Reference(string directory, string reference, params string[] arguments)
    {
        var result = processes.Run("git", arguments, directory);
        if (result.ExitCode != 0)
            throw new InvalidOperationException(
                $"--changed-since '{reference}' needs a reference git knows (in CI, check out with fetch-depth: 0): {result.Combined.Trim()}");
        return result.Output;
    }

    private string Git(string directory, params string[] arguments)
    {
        var result = processes.Run("git", arguments, directory);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"--changed needs a git repository: {result.Combined.Trim()}");
        return result.Output;
    }
}
