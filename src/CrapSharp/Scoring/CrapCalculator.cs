namespace CrapSharp.Scoring;

public static class CrapCalculator
{
    /// <summary>CRAP = CC² × (1 − coverage)³ + CC, with coverage as a fraction between 0 and 1.</summary>
    public static double Score(int complexity, double coverage)
    {
        var uncovered = 1 - Math.Clamp(coverage, 0, 1);
        return complexity * complexity * Math.Pow(uncovered, 3) + complexity;
    }
}
