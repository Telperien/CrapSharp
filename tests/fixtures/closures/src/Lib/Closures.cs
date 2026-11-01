namespace Lib;

public static class Closures
{
    public static int Score(int[] values, bool strict)
    {
        int Weight(int v)
        {
            if (v > 10)
                return 2;
            return 1;
        }
        Func<int, int> clamp = v =>
        {
            if (v < 0)
                return 0;
            return v;
        };
        var total = 0;
        foreach (var v in values)
            total += strict ? clamp(Weight(v)) : Weight(v);
        return total;
    }

    public static async Task<int> LoadAsync(bool fail)
    {
        await Task.Yield();
        if (fail)
            return -1;
        return 1;
    }

    public static IEnumerable<int> Evens(int limit)
    {
        for (var i = 0; i < limit; i++)
        {
            if (i % 2 == 0)
                yield return i;
        }
    }
}
