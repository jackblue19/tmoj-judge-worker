using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("role")]
[Index("RoleCode", Name = "role_role_code_key", IsUnique = true)]
public partial class Role
{
    [Key]
    [Column("role_id")]
    public Guid RoleId { get; set; }

    [Column("role_code")]
    public string RoleCode { get; set; } = null!;

    [Column("role_desc")]
    public string? RoleDesc { get; set; }

    [Column("is_system")]
    public bool IsSystem { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("Role")]
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    [InverseProperty("Role")]
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    [InverseProperty("Role")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
