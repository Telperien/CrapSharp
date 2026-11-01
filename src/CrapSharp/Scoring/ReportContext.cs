namespace CrapSharp.Scoring;

/// <summary>What a report covers and where its coverage came from, so a partial report cannot pass for a full one.</summary>
public sealed record ReportContext(
    bool Changed, string? Filter, CoverageMode CoverageSource, IReadOnlyList<string> CoverageReports, int FilesAnalyzed,
    IReadOnlyList<string>? ExcludedFragments = null)
{
    /// <summary>The --exclude fragments that left files out of this report.</summary>
    public IReadOnlyList<string> Excluded { get; } = ExcludedFragments ?? [];

    /// <summary>False when --changed, --filter or --exclude left some of the sources out.</summary>
    public bool Complete => !Changed && Filter is null && Excluded.Count == 0;
}
