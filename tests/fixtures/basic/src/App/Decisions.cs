namespace App;

public static class Decisions
{
    // Nine independent ifs: CC 10. Fully covered, the score is exactly 10.
    public static int Ten(int x)
    {
        var n = 0;
        if (x > 0) n++;
        if (x > 1) n++;
        if (x > 2) n++;
        if (x > 3) n++;
        if (x > 4) n++;
        if (x > 5) n++;
        if (x > 6) n++;
        if (x > 7) n++;
        if (x > 8) n++;
        return n;
    }

    // Never called by the tests: 0 % covered, CC 2.
    public static int Untouched(int x)
    {
        if (x > 0)
            return 1;
        return 0;
    }
}
