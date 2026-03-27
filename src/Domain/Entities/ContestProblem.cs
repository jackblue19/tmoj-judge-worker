using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ContestProblem
{
    public Guid Id { get; set; }

    public Guid ContestId { get; set; }

    public Guid ProblemId { get; set; }

    public int? Ordinal { get; set; }

    public string? Alias { get; set; }

    public int? DisplayIndex { get; set; }

    public int? MaxScore { get; set; }

    public Guid? OverrideTestsetId { get; set; }

    public int? TimeLimitMs { get; set; }

    public int? MemoryLimitKb { get; set; }

    public int? PenaltyPerWrong { get; set; }

    public string? ScoringCode { get; set; }

    public int? OutputLimitKb { get; set; }

    public int? Points { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public virtual Contest Contest { get; set; } = null!;

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Testset? OverrideTestset { get; set; }

    public virtual Problem Problem { get; set; } = null!;

    public virtual ICollection<ScoreRecalcJob> ScoreRecalcJobs { get; set; } = new List<ScoreRecalcJob>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
