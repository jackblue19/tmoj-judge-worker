using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("editorials")]
public partial class Editorial
{
    [Key]
    [Column("editorial_id")]
    public Guid EditorialId { get; set; }

    [Column("problem_id")]
    public Guid ProblemId { get; set; }

    [Column("author_id")]
    public Guid? AuthorId { get; set; }

    [Column("storage_id")]
    public Guid StorageId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("AuthorId")]
    [InverseProperty("Editorials")]
    public virtual User? Author { get; set; }

    [ForeignKey("StorageId")]
    [InverseProperty("Editorials")]
    public virtual StorageFile Storage { get; set; } = null!;
}
