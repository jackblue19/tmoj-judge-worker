using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Testcase
{
    public Guid Id { get; set; }

    public Guid TestsetId { get; set; }

    public int Ordinal { get; set; }

    public int Weight { get; set; }

    public bool IsSample { get; set; }

    public Guid? StorageBlobId { get; set; }

    public DateTime? ExpireAt { get; set; }

    public string? Input { get; set; }

    public string? ExpectedOutput { get; set; }

    public virtual ICollection<Result> Results { get; set; } = new List<Result>();

    public virtual ArtifactBlob? StorageBlob { get; set; }

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    public virtual Testset Testset { get; set; } = null!;
}
