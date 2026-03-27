using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class SubmissionQuotum
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid ProblemId { get; set; }

    public DateOnly Date { get; set; }

    public int Count { get; set; }

    public int QuotaLimit { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Problem Problem { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
