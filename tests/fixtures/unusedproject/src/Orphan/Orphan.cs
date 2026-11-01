namespace Orphan;

// No test project references this assembly, so coverlet never instruments it.
public static class Calculator
{
    public static int Positive(int x)
    {
        if (x > 0)
            return 1;
        return 0;
    }
}
