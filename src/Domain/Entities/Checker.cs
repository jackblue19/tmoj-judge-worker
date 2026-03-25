using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Checker
{
    public Guid Id { get; set; }

    public Guid ProblemId { get; set; }

    public Guid? ProblemTestsetId { get; set; }

    public string Type { get; set; } = null!;

    public Guid? SourceArtifactId { get; set; }

    public Guid? BinaryArtifactId { get; set; }

    public string? Entrypoint { get; set; }

    public bool IsActive { get; set; }

    public virtual ArtifactBlob? BinaryArtifact { get; set; }

    public virtual Problem Problem { get; set; } = null!;

    public virtual Testset? ProblemTestset { get; set; }

    public virtual ArtifactBlob? SourceArtifact { get; set; }
}
