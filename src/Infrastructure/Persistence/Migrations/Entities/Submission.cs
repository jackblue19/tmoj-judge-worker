using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("submissions")]
public partial class Submission
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("problem_id")]
    public Guid ProblemId { get; set; }

    [Column("runtime_id")]
    public Guid? RuntimeId { get; set; }

    [Column("code_artifact_id")]
    public Guid? CodeArtifactId { get; set; }

    [Column("code_size")]
    public int CodeSize { get; set; }

    [Column("code_hash")]
    public string CodeHash { get; set; } = null!;

    [Column("status_code")]
    public string StatusCode { get; set; } = null!;

    [Column("verdict_code")]
    public string? VerdictCode { get; set; }

    [Column("final_score")]
    [Precision(18, 2)]
    public decimal? FinalScore { get; set; }

    [Column("time_ms")]
    public int? TimeMs { get; set; }

    [Column("memory_kb")]
    public int? MemoryKb { get; set; }

    [Column("judged_at")]
    public DateTime? JudgedAt { get; set; }

    [Column("testset_id")]
    public Guid? TestsetId { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("team_id")]
    public Guid? TeamId { get; set; }

    [Column("contest_problem_id")]
    public Guid? ContestProblemId { get; set; }

    [Column("testcase_id")]
    public Guid? TestcaseId { get; set; }

    [Column("custom_input")]
    public string? CustomInput { get; set; }

    [Column("type")]
    public string? Type { get; set; }

    [Column("storage_blob_id")]
    public Guid? StorageBlobId { get; set; }

    [Column("submission_type")]
    public string SubmissionType { get; set; } = null!;

    [Column("ip_address")]
    public IPAddress? IpAddress { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [ForeignKey("CodeArtifactId")]
    [InverseProperty("SubmissionCodeArtifacts")]
    public virtual ArtifactBlob? CodeArtifact { get; set; }

    [ForeignKey("ContestProblemId")]
    [InverseProperty("Submissions")]
    public virtual ContestProblem? ContestProblem { get; set; }

    [InverseProperty("BestSubmission")]
    public virtual ICollection<ContestScoreboard> ContestScoreboardBestSubmissions { get; set; } = new List<ContestScoreboard>();

    [InverseProperty("LastSubmission")]
    public virtual ICollection<ContestScoreboard> ContestScoreboardLastSubmissions { get; set; } = new List<ContestScoreboard>();

    [InverseProperty("Submission")]
    public virtual ICollection<JudgeJob> JudgeJobs { get; set; } = new List<JudgeJob>();

    [InverseProperty("Submission")]
    public virtual ICollection<JudgeRun> JudgeRuns { get; set; } = new List<JudgeRun>();

    [ForeignKey("ProblemId")]
    [InverseProperty("Submissions")]
    public virtual Problem Problem { get; set; } = null!;

    [InverseProperty("Submission")]
    public virtual ICollection<Result> Results { get; set; } = new List<Result>();

    [ForeignKey("RuntimeId")]
    [InverseProperty("Submissions")]
    public virtual Runtime? Runtime { get; set; }

    [ForeignKey("StorageBlobId")]
    [InverseProperty("SubmissionStorageBlobs")]
    public virtual ArtifactBlob? StorageBlob { get; set; }

    [ForeignKey("TeamId")]
    [InverseProperty("Submissions")]
    public virtual Team? Team { get; set; }

    [ForeignKey("TestcaseId")]
    [InverseProperty("Submissions")]
    public virtual Testcase? Testcase { get; set; }

    [ForeignKey("TestsetId")]
    [InverseProperty("Submissions")]
    public virtual Testset? Testset { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Submissions")]
    public virtual User User { get; set; } = null!;

    [InverseProperty("BestSubmission")]
    public virtual ICollection<UserProblemStat> UserProblemStats { get; set; } = new List<UserProblemStat>();
}
