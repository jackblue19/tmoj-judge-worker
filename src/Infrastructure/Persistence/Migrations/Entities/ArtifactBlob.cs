using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("artifact_blobs")]
[Index("Sha256", "SizeBytes", Name = "artifact_blobs_sha256_size_bytes_key", IsUnique = true)]
public partial class ArtifactBlob
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("sha256")]
    public string Sha256 { get; set; } = null!;

    [Column("size_bytes")]
    public long SizeBytes { get; set; }

    [Column("content_type")]
    public string? ContentType { get; set; }

    [Column("storage_uri")]
    public string StorageUri { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("BinaryArtifact")]
    public virtual ICollection<Checker> CheckerBinaryArtifacts { get; set; } = new List<Checker>();

    [InverseProperty("SourceArtifact")]
    public virtual ICollection<Checker> CheckerSourceArtifacts { get; set; } = new List<Checker>();

    [InverseProperty("CompileLogBlob")]
    public virtual ICollection<JudgeRun> JudgeRuns { get; set; } = new List<JudgeRun>();

    [InverseProperty("StderrBlob")]
    public virtual ICollection<Result> ResultStderrBlobs { get; set; } = new List<Result>();

    [InverseProperty("StdoutBlob")]
    public virtual ICollection<Result> ResultStdoutBlobs { get; set; } = new List<Result>();

    [InverseProperty("CodeArtifact")]
    public virtual ICollection<Submission> SubmissionCodeArtifacts { get; set; } = new List<Submission>();

    [InverseProperty("StorageBlob")]
    public virtual ICollection<Submission> SubmissionStorageBlobs { get; set; } = new List<Submission>();

    [InverseProperty("StorageBlob")]
    public virtual ICollection<Testcase> Testcases { get; set; } = new List<Testcase>();

    [InverseProperty("StorageBlob")]
    public virtual ICollection<Testset> Testsets { get; set; } = new List<Testset>();
}
