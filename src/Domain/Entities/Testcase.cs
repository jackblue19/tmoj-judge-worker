using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("testcases")]
[Index("TestsetId", "Ordinal", Name = "testcases_testset_id_ordinal_key", IsUnique = true)]
public partial class Testcase
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("testset_id")]
    public Guid TestsetId { get; set; }

    [Column("ordinal")]
    public int Ordinal { get; set; }

    [Column("weight")]
    public int Weight { get; set; }

    [Column("is_sample")]
    public bool IsSample { get; set; }

    [Column("storage_blob_id")]
    public Guid? StorageBlobId { get; set; }

    [Column("expire_at")]
    public DateTime? ExpireAt { get; set; }

    [Column("input")]
    public string? Input { get; set; }

    [Column("expected_output")]
    public string? ExpectedOutput { get; set; }

    [InverseProperty("Testcase")]
    public virtual ICollection<Result> Results { get; set; } = new List<Result>();

    [ForeignKey("StorageBlobId")]
    [InverseProperty("Testcases")]
    public virtual ArtifactBlob? StorageBlob { get; set; }

    [InverseProperty("Testcase")]
    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    [ForeignKey("TestsetId")]
    [InverseProperty("Testcases")]
    public virtual Testset Testset { get; set; } = null!;
}
