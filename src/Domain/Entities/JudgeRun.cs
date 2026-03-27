using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class JudgeRun
{
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }

    public Guid? WorkerId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public string Status { get; set; } = null!;

    public Guid? RuntimeId { get; set; }

    public string? DockerImage { get; set; }

    public string? Limits { get; set; }

    public string? Note { get; set; }

    public Guid? CompileLogBlobId { get; set; }

    public int? CompileExitCode { get; set; }

    public int? CompileTimeMs { get; set; }

    public int? TotalTimeMs { get; set; }

    public int? TotalMemoryKb { get; set; }

    public virtual ArtifactBlob? CompileLogBlob { get; set; }

    public virtual ICollection<Result> Results { get; set; } = new List<Result>();

    public virtual Runtime? Runtime { get; set; }

    public virtual Submission Submission { get; set; } = null!;

    public virtual JudgeWorker? Worker { get; set; }
}
