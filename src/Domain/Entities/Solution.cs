using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Solution
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid ProblemId { get; set; }

    public Guid? RuntimeId { get; set; }

    public string? DescMd { get; set; }

    public string? Note { get; set; }

    public virtual Problem Problem { get; set; } = null!;

    public virtual Runtime? Runtime { get; set; }

    public virtual User User { get; set; } = null!;
}
