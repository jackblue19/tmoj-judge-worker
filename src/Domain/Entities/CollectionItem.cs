using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("collection_items")]
public partial class CollectionItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("collection_id")]
    public Guid CollectionId { get; set; }

    [Column("problem_id")]
    public Guid? ProblemId { get; set; }

    [Column("contest_id")]
    public Guid? ContestId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("CollectionId")]
    [InverseProperty("CollectionItems")]
    public virtual Collection Collection { get; set; } = null!;

    [ForeignKey("ContestId")]
    [InverseProperty("CollectionItems")]
    public virtual Contest? Contest { get; set; }

    [ForeignKey("ProblemId")]
    [InverseProperty("CollectionItems")]
    public virtual Problem? Problem { get; set; }
}
