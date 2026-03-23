using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[PrimaryKey("RoleId", "PermissionId")]
[Table("role_permission")]
public partial class RolePermission
{
    [Key]
    [Column("role_id")]
    public Guid RoleId { get; set; }

    [Key]
    [Column("permission_id")]
    public Guid PermissionId { get; set; }

    [Column("status")]
    public bool Status { get; set; }

    [ForeignKey("PermissionId")]
    [InverseProperty("RolePermissions")]
    public virtual Permission Permission { get; set; } = null!;

    [ForeignKey("RoleId")]
    [InverseProperty("RolePermissions")]
    public virtual Role Role { get; set; } = null!;
}
