namespace CrapSharp.Scoring;

/// <summary>
/// The goal is every score strictly below the threshold (ub-mode: "under CRAP 10"), so a score equal to it fails.
/// The raw score is compared, never the displayed one.
/// </summary>
public static class ThresholdCheck
{
    /// <summary>The methods scoring the threshold or more. Reports without a score never fail.</summary>
    public static IReadOnlyList<MethodReport> Failing(IEnumerable<MethodReport> reports, double threshold) =>
        reports.Where(r => r.Crap >= threshold).ToList();
}
