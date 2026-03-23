using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("judge_workers")]
public partial class JudgeWorker
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("capabilities", TypeName = "jsonb")]
    public string? Capabilities { get; set; }

    [Column("last_seen_at")]
    public DateTime? LastSeenAt { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("version")]
    public string? Version { get; set; }

    [InverseProperty("DequeuedByWorker")]
    public virtual ICollection<JudgeJob> JudgeJobs { get; set; } = new List<JudgeJob>();

    [InverseProperty("Worker")]
    public virtual ICollection<JudgeRun> JudgeRuns { get; set; } = new List<JudgeRun>();
}
