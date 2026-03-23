using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("problem_editorials")]
[Index("ProblemId", Name = "problem_editorials_problem_id_key", IsUnique = true)]
public partial class ProblemEditorial
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("problem_id")]
    public Guid ProblemId { get; set; }

    [Column("author_id")]
    public Guid AuthorId { get; set; }

    [Column("content")]
    public string Content { get; set; } = null!;

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("AuthorId")]
    [InverseProperty("ProblemEditorials")]
    public virtual User Author { get; set; } = null!;

    [ForeignKey("ProblemId")]
    [InverseProperty("ProblemEditorial")]
    public virtual Problem Problem { get; set; } = null!;
}
