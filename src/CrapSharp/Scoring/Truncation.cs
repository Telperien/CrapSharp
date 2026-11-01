namespace CrapSharp.Scoring;

/// <summary>
/// Displayed numbers are cut down, never rounded up: a displayed 10.00 really is 10 or more and a displayed 9.99 is
/// below 10, so what the user reads agrees with the threshold check (done on the raw score).
/// </summary>
public static class Truncation
{
    // Absorbs the binary error of values such as 2.5 * 100 = 249.99999...
    private const double Tolerance = 1e-9;

    public static double Down(double value, int decimals)
    {
        var factor = Math.Pow(10, decimals);
        return Math.Floor(value * factor + Tolerance) / factor;
    }
}
