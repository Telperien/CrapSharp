namespace CrapSharp;

/// <summary>Process exit codes. When several apply, the most serious wins: Error, then TestsFailed, then ThresholdNotMet.</summary>
public static class ExitCodes
{
    public const int Ok = 0;

    /// <summary>Bad arguments, missing path, no usable coverage report...: nothing was scored.</summary>
    public const int Error = 1;

    /// <summary>--threshold N: at least one method scores N or more.</summary>
    public const int ThresholdNotMet = 2;

    /// <summary>`dotnet test` or the coverage command failed: the report is still printed, but its coverage may be incomplete.</summary>
    public const int TestsFailed = 3;
}
