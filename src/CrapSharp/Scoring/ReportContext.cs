namespace CrapSharp.Scoring;

/// <summary>What a report covers and where its coverage came from, so a partial report cannot pass for a full one.</summary>
public sealed record ReportContext(
    bool Changed, string? Filter, CoverageMode CoverageSource, IReadOnlyList<string> CoverageReports, int FilesAnalyzed)
{
    /// <summary>False when --changed or --filter left some of the sources out.</summary>
    public bool Complete => !Changed && Filter is null;
}
