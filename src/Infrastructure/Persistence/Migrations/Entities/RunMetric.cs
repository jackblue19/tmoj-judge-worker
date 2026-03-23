using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("run_metrics")]
public partial class RunMetric
{
    [Key]
    [Column("metric_id")]
    public Guid MetricId { get; set; }

    [Column("submission_id")]
    public Guid SubmissionId { get; set; }

    [Column("runtime_ms")]
    public int? RuntimeMs { get; set; }

    [Column("memory_kb")]
    public int? MemoryKb { get; set; }

    [Column("cpu_usage")]
    [Precision(5, 2)]
    public decimal? CpuUsage { get; set; }

    [Column("passed_testcases")]
    public int? PassedTestcases { get; set; }

    [Column("total_testcases")]
    public int? TotalTestcases { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
