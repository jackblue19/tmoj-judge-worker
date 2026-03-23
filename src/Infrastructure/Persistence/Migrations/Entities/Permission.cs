using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("permission")]
[Index("PermissionCode", Name = "permission_permission_code_key", IsUnique = true)]
public partial class Permission
{
    [Key]
    [Column("permission_id")]
    public Guid PermissionId { get; set; }

    [Column("permission_code")]
    public string PermissionCode { get; set; } = null!;

    [Column("permission_desc")]
    public string? PermissionDesc { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [InverseProperty("Permission")]
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    [InverseProperty("Permission")]
    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
