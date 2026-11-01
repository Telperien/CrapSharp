using System.Diagnostics.CodeAnalysis;

namespace Lib;

public class Rules
{
    public int Measured(int x)
    {
        if (x > 0)
            return 1;
        return 0;
    }

    [ExcludeFromCodeCoverage]
    public int ExcludedMethod(int x)
    {
        if (x > 0 && x < 10)
            return 1;
        return 0;
    }

    [ExcludeFromCodeCoverage]
    public int ExcludedProperty
    {
        get { return Measured(1) > 0 ? 1 : 0; }
    }
}

[ExcludeFromCodeCoverage]
public class ExcludedType
{
    public int Never(int x)
    {
        if (x > 0)
            return 1;
        return 0;
    }
}
