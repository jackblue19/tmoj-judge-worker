using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Result
{
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }

    public Guid? JudgeRunId { get; set; }

    public Guid? TestcaseId { get; set; }

    public string? StatusCode { get; set; }

    public int? RuntimeMs { get; set; }

    public int? MemoryKb { get; set; }

    public string? Input { get; set; }

    public string? ExpectedOutput { get; set; }

    public string? ActualOutput { get; set; }

    public Guid? StdoutBlobId { get; set; }

    public Guid? StderrBlobId { get; set; }

    public string? CheckerMessage { get; set; }

    public int? ExitCode { get; set; }

    public int? Signal { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? OutputUrl { get; set; }

    public string? Type { get; set; }

    public string? Message { get; set; }

    public string? Note { get; set; }

    public virtual JudgeRun? JudgeRun { get; set; }

    public virtual ArtifactBlob? StderrBlob { get; set; }

    public virtual ArtifactBlob? StdoutBlob { get; set; }

    public virtual Submission Submission { get; set; } = null!;

    public virtual Testcase? Testcase { get; set; }
}
