using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("judge_jobs")]
public partial class JudgeJob
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("submission_id")]
    public Guid SubmissionId { get; set; }

    [Column("enqueue_at")]
    public DateTime EnqueueAt { get; set; }

    [Column("dequeued_by_worker_id")]
    public Guid? DequeuedByWorkerId { get; set; }

    [Column("dequeued_at")]
    public DateTime? DequeuedAt { get; set; }

    [Column("status")]
    public string Status { get; set; } = null!;

    [Column("attempts")]
    public int Attempts { get; set; }

    [Column("last_error")]
    public string? LastError { get; set; }

    [Column("priority")]
    public int Priority { get; set; }

    [Column("triggered_by_user_id")]
    public Guid? TriggeredByUserId { get; set; }

    [Column("trigger_type")]
    public string TriggerType { get; set; } = null!;

    [Column("trigger_reason")]
    public string? TriggerReason { get; set; }

    [ForeignKey("DequeuedByWorkerId")]
    [InverseProperty("JudgeJobs")]
    public virtual JudgeWorker? DequeuedByWorker { get; set; }

    [ForeignKey("SubmissionId")]
    [InverseProperty("JudgeJobs")]
    public virtual Submission Submission { get; set; } = null!;

    [ForeignKey("TriggeredByUserId")]
    [InverseProperty("JudgeJobs")]
    public virtual User? TriggeredByUser { get; set; }
}
