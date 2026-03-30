using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ContestScoreboard
{
    public Guid ContestId { get; set; }

    public Guid EntryId { get; set; }

    public Guid ProblemId { get; set; }

    public bool AcmSolved { get; set; }

    public int AcmAttempts { get; set; }

    public int AcmPenaltyTime { get; set; }

    public DateTime? FirstAcAt { get; set; }

    public decimal BestScore { get; set; }

    public decimal LastScore { get; set; }

    public Guid? BestSubmissionId { get; set; }

    public Guid? LastSubmissionId { get; set; }

    public DateTime? LastSubmitAt { get; set; }

    public DateTime? VisibleUntil { get; set; }

    public virtual Submission? BestSubmission { get; set; }

    public virtual Contest Contest { get; set; } = null!;

    public virtual ContestTeam Entry { get; set; } = null!;

    public virtual Submission? LastSubmission { get; set; }

    public virtual Problem Problem { get; set; } = null!;
}
