using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class UserProblemStat
{
    public Guid UserId { get; set; }

    public Guid ProblemId { get; set; }

    public int Attempts { get; set; }

    public bool Solved { get; set; }

    public Guid? BestSubmissionId { get; set; }

    public DateTime? LastSubmissionAt { get; set; }

    public virtual Submission? BestSubmission { get; set; }

    public virtual Problem Problem { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
