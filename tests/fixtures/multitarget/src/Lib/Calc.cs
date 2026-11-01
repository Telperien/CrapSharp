namespace Lib;

public static class Calc
{
    public static int Abs(int x)
    {
        if (x < 0)
            return -x;
        return x;
    }

    public static int Twice(int x)
    {
        if (x == int.MaxValue)
            return x;
        return x * 2;
    }
}
