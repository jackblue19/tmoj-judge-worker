using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("runtime")]
public partial class Runtime
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("runtime_name")]
    public string RuntimeName { get; set; } = null!;

    [Column("runtime_version")]
    public string? RuntimeVersion { get; set; }

    [Column("image_ref")]
    public string? ImageRef { get; set; }

    [Column("default_time_limit_ms")]
    public int DefaultTimeLimitMs { get; set; }

    [Column("default_memory_limit_kb")]
    public int DefaultMemoryLimitKb { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [InverseProperty("Runtime")]
    public virtual ICollection<JudgeRun> JudgeRuns { get; set; } = new List<JudgeRun>();

    [InverseProperty("Runtime")]
    public virtual ICollection<Solution> Solutions { get; set; } = new List<Solution>();

    [InverseProperty("Runtime")]
    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
