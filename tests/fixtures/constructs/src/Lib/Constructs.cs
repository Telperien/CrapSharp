namespace Lib;

// One method per decision construct, only compiled: the complexity of each is cross-checked with an independent analyzer.
public static class Constructs
{
    public static int IfElse(int x)
    {
        if (x > 0)
            return 1;
        else if (x < 0)
            return -1;
        else
            return 0;
    }

    public static int Loops(int[] values)
    {
        var total = 0;
        foreach (var v in values)
            total += v;
        for (var i = 0; i < 3; i++)
            total++;
        while (total < 10)
            total++;
        do
        {
            total++;
        }
        while (total < 20);
        return total;
    }

    public static int Catches(string text)
    {
        try
        {
            return int.Parse(text);
        }
        catch (FormatException)
        {
            return -1;
        }
        catch (OverflowException)
        {
            return -2;
        }
        finally
        {
            Console.Write("done");
        }
    }

    public static int SwitchStatement(int x)
    {
        switch (x)
        {
            case 1:
                return 1;
            case 2:
                return 2;
            case 3:
            case 4:
                return 3;
            default:
                return 0;
        }
    }

    public static int SwitchExpression(int x) => x switch { 1 => 1, 2 => 2, _ => 0 };

    public static int WhenGuard(object value)
    {
        switch (value)
        {
            case int n when n > 0:
                return n;
            default:
                return 0;
        }
    }

    public static int Ternary(int x) => x > 0 ? 1 : 0;

    public static bool Logic(bool a, bool b, bool c) => a && b || c;

    public static string Coalesce(string? first, string? second)
    {
        var result = first ?? second ?? "a";
        result ??= "b";
        return result;
    }

    public static int? NullConditional(string? text) => text?.Trim()?.Length;

    public static bool OrPattern(string text) => text is "a" or "b";

    public static bool NotPattern(string? text) => text is not null;

    public static IEnumerable<int> Lambda(int[] values) => values.Where(v => v > 0 && v < 10);
}
