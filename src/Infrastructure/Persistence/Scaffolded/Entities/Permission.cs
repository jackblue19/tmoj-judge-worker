using System;
using System.Collections.Generic;

Domain.Entities

public partial class Permission
{
    public Guid PermissionId { get; set; }

    public string PermissionCode { get; set; } = null!;

    public string? PermissionDesc { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
