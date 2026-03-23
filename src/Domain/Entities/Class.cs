using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("class")]
[Index("ClassCode", Name = "class_class_code_key", IsUnique = true)]
[Index("InviteCode", Name = "class_invite_code_key", IsUnique = true)]
public partial class Class
{
    [Key]
    [Column("class_id")]
    public Guid ClassId { get; set; }

    [Column("subject_id")]
    public Guid SubjectId { get; set; }

    [Column("semester_id")]
    public Guid SemesterId { get; set; }

    [Column("class_code")]
    public string ClassCode { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("start_date")]
    public DateOnly? StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("teacher_id")]
    public Guid? TeacherId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("invite_code")]
    public string? InviteCode { get; set; }

    [Column("invite_code_expires_at")]
    public DateTime? InviteCodeExpiresAt { get; set; }

    [InverseProperty("Class")]
    public virtual ICollection<ClassMember> ClassMembers { get; set; } = new List<ClassMember>();

    [InverseProperty("Class")]
    public virtual ICollection<ClassSlot> ClassSlots { get; set; } = new List<ClassSlot>();

    [ForeignKey("SemesterId")]
    [InverseProperty("Classes")]
    public virtual Semester Semester { get; set; } = null!;

    [ForeignKey("SubjectId")]
    [InverseProperty("Classes")]
    public virtual Subject Subject { get; set; } = null!;

    [ForeignKey("TeacherId")]
    [InverseProperty("Classes")]
    public virtual User? Teacher { get; set; }
}
