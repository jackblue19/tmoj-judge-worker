using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("testsets")]
public partial class Testset
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("problem_id")]
    public Guid ProblemId { get; set; }

    [Column("type")]
    public string Type { get; set; } = null!;

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("storage_blob_id")]
    public Guid? StorageBlobId { get; set; }

    [Column("expire_at")]
    public DateTime? ExpireAt { get; set; }

    [InverseProperty("ProblemTestset")]
    public virtual ICollection<Checker> Checkers { get; set; } = new List<Checker>();

    [InverseProperty("OverrideTestset")]
    public virtual ICollection<ContestProblem> ContestProblems { get; set; } = new List<ContestProblem>();

    [ForeignKey("CreatedBy")]
    [InverseProperty("Testsets")]
    public virtual User? CreatedByNavigation { get; set; }

    [ForeignKey("ProblemId")]
    [InverseProperty("Testsets")]
    public virtual Problem Problem { get; set; } = null!;

    [ForeignKey("StorageBlobId")]
    [InverseProperty("Testsets")]
    public virtual ArtifactBlob? StorageBlob { get; set; }

    [InverseProperty("Testset")]
    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    [InverseProperty("Testset")]
    public virtual ICollection<Testcase> Testcases { get; set; } = new List<Testcase>();
}
