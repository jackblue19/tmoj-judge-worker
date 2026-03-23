using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("solution")]
[Index("UserId", "ProblemId", Name = "solution_user_id_problem_id_key", IsUnique = true)]
public partial class Solution
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

    [Column("desc_md")]
    public string? DescMd { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [ForeignKey("ProblemId")]
    [InverseProperty("Solutions")]
    public virtual Problem Problem { get; set; } = null!;

    [ForeignKey("RuntimeId")]
    [InverseProperty("Solutions")]
    public virtual Runtime? Runtime { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Solutions")]
    public virtual User User { get; set; } = null!;
}
