using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Testset
{
    public Guid Id { get; set; }

    public Guid ProblemId { get; set; }

    public string Type { get; set; } = null!;

    public bool IsActive { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? StorageBlobId { get; set; }

    public DateTime? ExpireAt { get; set; }

    public virtual ICollection<Checker> Checkers { get; set; } = new List<Checker>();

    public virtual ICollection<ContestProblem> ContestProblems { get; set; } = new List<ContestProblem>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Problem Problem { get; set; } = null!;

    public virtual ArtifactBlob? StorageBlob { get; set; }

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    public virtual ICollection<Testcase> Testcases { get; set; } = new List<Testcase>();
}
