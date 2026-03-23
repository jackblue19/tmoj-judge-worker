using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[PrimaryKey("UserId", "RoleId")]
[Table("user_role")]
public partial class UserRole
{
    [Key]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Key]
    [Column("role_id")]
    public Guid RoleId { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; }

    [Column("assigned_by")]
    public Guid? AssignedBy { get; set; }

    [ForeignKey("AssignedBy")]
    [InverseProperty("UserRoleAssignedByNavigations")]
    public virtual User? AssignedByNavigation { get; set; }

    [ForeignKey("RoleId")]
    [InverseProperty("UserRoles")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("UserRoleUsers")]
    public virtual User User { get; set; } = null!;
}
