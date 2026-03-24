using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("result")]
public partial class Result
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("submission_id")]
    public Guid SubmissionId { get; set; }

    [Column("judge_run_id")]
    public Guid? JudgeRunId { get; set; }

    [Column("testcase_id")]
    public Guid? TestcaseId { get; set; }

    [Column("status_code")]
    public string? StatusCode { get; set; }

    [Column("runtime_ms")]
    public int? RuntimeMs { get; set; }

    [Column("memory_kb")]
    public int? MemoryKb { get; set; }

    [Column("input")]
    public string? Input { get; set; }

    [Column("expected_output")]
    public string? ExpectedOutput { get; set; }

    [Column("actual_output")]
    public string? ActualOutput { get; set; }

    [Column("stdout_blob_id")]
    public Guid? StdoutBlobId { get; set; }

    [Column("stderr_blob_id")]
    public Guid? StderrBlobId { get; set; }

    [Column("checker_message")]
    public string? CheckerMessage { get; set; }

    [Column("exit_code")]
    public int? ExitCode { get; set; }

    [Column("signal")]
    public int? Signal { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("output_url")]
    public string? OutputUrl { get; set; }

    [Column("type")]
    public string? Type { get; set; }

    [Column("message")]
    public string? Message { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [ForeignKey("JudgeRunId")]
    [InverseProperty("Results")]
    public virtual JudgeRun? JudgeRun { get; set; }

    [ForeignKey("StderrBlobId")]
    [InverseProperty("ResultStderrBlobs")]
    public virtual ArtifactBlob? StderrBlob { get; set; }

    [ForeignKey("StdoutBlobId")]
    [InverseProperty("ResultStdoutBlobs")]
    public virtual ArtifactBlob? StdoutBlob { get; set; }

    [ForeignKey("SubmissionId")]
    [InverseProperty("Results")]
    public virtual Submission Submission { get; set; } = null!;

    [ForeignKey("TestcaseId")]
    [InverseProperty("Results")]
    public virtual Testcase? Testcase { get; set; }
}
