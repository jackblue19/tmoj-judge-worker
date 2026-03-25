using System;
using System.Collections.Generic;
using System.Net;

namespace Domain.Entities;

public partial class Submission
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid ProblemId { get; set; }

    public Guid? RuntimeId { get; set; }

    public Guid? CodeArtifactId { get; set; }

    public int CodeSize { get; set; }

    public string CodeHash { get; set; } = null!;

    public string StatusCode { get; set; } = null!;

    public string? VerdictCode { get; set; }

    public decimal? FinalScore { get; set; }

    public int? TimeMs { get; set; }

    public int? MemoryKb { get; set; }

    public DateTime? JudgedAt { get; set; }

    public Guid? TestsetId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? TeamId { get; set; }

    public Guid? ContestProblemId { get; set; }

    public Guid? TestcaseId { get; set; }

    public string? CustomInput { get; set; }

    public string? Type { get; set; }

    public Guid? StorageBlobId { get; set; }

    public string SubmissionType { get; set; } = null!;

    public IPAddress? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public virtual ArtifactBlob? CodeArtifact { get; set; }

    public virtual ContestProblem? ContestProblem { get; set; }

    public virtual ICollection<ContestScoreboard> ContestScoreboardBestSubmissions { get; set; } = new List<ContestScoreboard>();

    public virtual ICollection<ContestScoreboard> ContestScoreboardLastSubmissions { get; set; } = new List<ContestScoreboard>();

    public virtual ICollection<JudgeJob> JudgeJobs { get; set; } = new List<JudgeJob>();

    public virtual ICollection<JudgeRun> JudgeRuns { get; set; } = new List<JudgeRun>();

    public virtual Problem Problem { get; set; } = null!;

    public virtual ICollection<Result> Results { get; set; } = new List<Result>();

    public virtual Runtime? Runtime { get; set; }

    public virtual ArtifactBlob? StorageBlob { get; set; }

    public virtual Team? Team { get; set; }

    public virtual Testcase? Testcase { get; set; }

    public virtual Testset? Testset { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ICollection<UserProblemStat> UserProblemStats { get; set; } = new List<UserProblemStat>();
}
