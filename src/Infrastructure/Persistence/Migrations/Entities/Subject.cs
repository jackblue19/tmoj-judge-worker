using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("subject")]
[Index("Code", Name = "subject_code_key", IsUnique = true)]
public partial class Subject
{
    [Key]
    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [Column("code")]
    public string Code { get; set; } = null!;

    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("Subject")]
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
