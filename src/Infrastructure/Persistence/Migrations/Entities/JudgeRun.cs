using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("judge_runs")]
public partial class JudgeRun
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("submission_id")]
    public Guid SubmissionId { get; set; }

    [Column("worker_id")]
    public Guid? WorkerId { get; set; }

    [Column("started_at")]
    public DateTime StartedAt { get; set; }

    [Column("finished_at")]
    public DateTime? FinishedAt { get; set; }

    [Column("status")]
    public string Status { get; set; } = null!;

    [Column("runtime_id")]
    public Guid? RuntimeId { get; set; }

    [Column("docker_image")]
    public string? DockerImage { get; set; }

    [Column("limits", TypeName = "jsonb")]
    public string? Limits { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("compile_log_blob_id")]
    public Guid? CompileLogBlobId { get; set; }

    [Column("compile_exit_code")]
    public int? CompileExitCode { get; set; }

    [Column("compile_time_ms")]
    public int? CompileTimeMs { get; set; }

    [Column("total_time_ms")]
    public int? TotalTimeMs { get; set; }

    [Column("total_memory_kb")]
    public int? TotalMemoryKb { get; set; }

    [ForeignKey("CompileLogBlobId")]
    [InverseProperty("JudgeRuns")]
    public virtual ArtifactBlob? CompileLogBlob { get; set; }

    [InverseProperty("JudgeRun")]
    public virtual ICollection<Result> Results { get; set; } = new List<Result>();

    [ForeignKey("RuntimeId")]
    [InverseProperty("JudgeRuns")]
    public virtual Runtime? Runtime { get; set; }

    [ForeignKey("SubmissionId")]
    [InverseProperty("JudgeRuns")]
    public virtual Submission Submission { get; set; } = null!;

    [ForeignKey("WorkerId")]
    [InverseProperty("JudgeRuns")]
    public virtual JudgeWorker? Worker { get; set; }
}
