namespace CrapSharp.Tests.EndToEnd;

/// <summary>
/// A test that publishes the tool and runs it on a real mini-solution (dotnet restore, build, test, coverlet):
/// minutes, not milliseconds. It is skipped unless CRAPSHARP_E2E=1, so a plain `dotnet test` stays fast.
/// </summary>
public sealed class EndToEndFactAttribute : FactAttribute
{
    public const string EnvironmentVariable = "CRAPSHARP_E2E";

    public EndToEndFactAttribute()
    {
        if (Environment.GetEnvironmentVariable(EnvironmentVariable) != "1")
            Skip = $"slow end-to-end test: set {EnvironmentVariable}=1 to run it";
    }
}

/// <summary>Theory counterpart of <see cref="EndToEndFactAttribute"/>.</summary>
public sealed class EndToEndTheoryAttribute : TheoryAttribute
{
    public EndToEndTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable(EndToEndFactAttribute.EnvironmentVariable) != "1")
            Skip = $"slow end-to-end test: set {EndToEndFactAttribute.EnvironmentVariable}=1 to run it";
    }
}
