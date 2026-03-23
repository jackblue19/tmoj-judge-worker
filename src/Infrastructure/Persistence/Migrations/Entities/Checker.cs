using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("checkers")]
public partial class Checker
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("problem_id")]
    public Guid ProblemId { get; set; }

    [Column("problem_testset_id")]
    public Guid? ProblemTestsetId { get; set; }

    [Column("type")]
    public string Type { get; set; } = null!;

    [Column("source_artifact_id")]
    public Guid? SourceArtifactId { get; set; }

    [Column("binary_artifact_id")]
    public Guid? BinaryArtifactId { get; set; }

    [Column("entrypoint")]
    public string? Entrypoint { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [ForeignKey("BinaryArtifactId")]
    [InverseProperty("CheckerBinaryArtifacts")]
    public virtual ArtifactBlob? BinaryArtifact { get; set; }

    [ForeignKey("ProblemId")]
    [InverseProperty("Checkers")]
    public virtual Problem Problem { get; set; } = null!;

    [ForeignKey("ProblemTestsetId")]
    [InverseProperty("Checkers")]
    public virtual Testset? ProblemTestset { get; set; }

    [ForeignKey("SourceArtifactId")]
    [InverseProperty("CheckerSourceArtifacts")]
    public virtual ArtifactBlob? SourceArtifact { get; set; }
}
