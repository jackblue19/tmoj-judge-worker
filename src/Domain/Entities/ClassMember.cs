using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("class_member")]
[Index("ClassId", "UserId", Name = "uq_class_user", IsUnique = true)]
public partial class ClassMember
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("class_id")]
    public Guid ClassId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("joined_at")]
    public DateTime JoinedAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [ForeignKey("ClassId")]
    [InverseProperty("ClassMembers")]
    public virtual Class Class { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("ClassMembers")]
    public virtual User User { get; set; } = null!;
}
