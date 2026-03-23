using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[PrimaryKey("UserId", "PermissionId")]
[Table("user_permission")]
public partial class UserPermission
{
    [Key]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Key]
    [Column("permission_id")]
    public Guid PermissionId { get; set; }

    [Column("effective_at")]
    public DateTime EffectiveAt { get; set; }

    [Column("expire_at")]
    public DateTime? ExpireAt { get; set; }

    [Column("status")]
    public bool Status { get; set; }

    [ForeignKey("PermissionId")]
    [InverseProperty("UserPermissions")]
    public virtual Permission Permission { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("UserPermissions")]
    public virtual User User { get; set; } = null!;
}
