using System;
using System.Collections.Generic;

Domain.Entities

public partial class RunMetric
{
    public Guid MetricId { get; set; }

    public Guid SubmissionId { get; set; }

    public int? RuntimeMs { get; set; }

    public int? MemoryKb { get; set; }

    public decimal? CpuUsage { get; set; }

    public int? PassedTestcases { get; set; }

    public int? TotalTestcases { get; set; }

    public DateTime CreatedAt { get; set; }
}
