using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Role
{
    public Guid RoleId { get; set; }

    public string RoleCode { get; set; } = null!;

    public string? RoleDesc { get; set; }

    public bool IsSystem { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
