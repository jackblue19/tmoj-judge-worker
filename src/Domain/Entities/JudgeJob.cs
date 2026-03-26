using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class JudgeJob
{
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }

    public DateTime EnqueueAt { get; set; }

    public Guid? DequeuedByWorkerId { get; set; }

    public DateTime? DequeuedAt { get; set; }

    public string Status { get; set; } = null!;

    public int Attempts { get; set; }

    public string? LastError { get; set; }

    public int Priority { get; set; }

    public Guid? TriggeredByUserId { get; set; }

    public string TriggerType { get; set; } = null!;

    public string? TriggerReason { get; set; }

    public virtual JudgeWorker? DequeuedByWorker { get; set; }

    public virtual Submission Submission { get; set; } = null!;

    public virtual User? TriggeredByUser { get; set; }
}
