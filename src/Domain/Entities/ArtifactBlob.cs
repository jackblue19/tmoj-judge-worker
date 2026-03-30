using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ArtifactBlob
{
    public Guid Id { get; set; }

    public string Sha256 { get; set; } = null!;

    public long SizeBytes { get; set; }

    public string? ContentType { get; set; }

    public string StorageUri { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Checker> CheckerBinaryArtifacts { get; set; } = new List<Checker>();

    public virtual ICollection<Checker> CheckerSourceArtifacts { get; set; } = new List<Checker>();

    public virtual ICollection<JudgeRun> JudgeRuns { get; set; } = new List<JudgeRun>();

    public virtual ICollection<Result> ResultStderrBlobs { get; set; } = new List<Result>();

    public virtual ICollection<Result> ResultStdoutBlobs { get; set; } = new List<Result>();

    public virtual ICollection<Submission> SubmissionCodeArtifacts { get; set; } = new List<Submission>();

    public virtual ICollection<Submission> SubmissionStorageBlobs { get; set; } = new List<Submission>();

    public virtual ICollection<Testcase> Testcases { get; set; } = new List<Testcase>();

    public virtual ICollection<Testset> Testsets { get; set; } = new List<Testset>();
}
