using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("semester")]
[Index("Code", Name = "semester_code_key", IsUnique = true)]
public partial class Semester
{
    [Key]
    [Column("semester_id")]
    public Guid SemesterId { get; set; }

    [Column("code")]
    public string Code { get; set; } = null!;

    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("start_at")]
    public DateOnly StartAt { get; set; }

    [Column("end_at")]
    public DateOnly EndAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("Semester")]
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
