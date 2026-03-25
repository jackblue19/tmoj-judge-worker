using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class RolePermission
{
    public Guid RoleId { get; set; }

    public Guid PermissionId { get; set; }

    public bool Status { get; set; }

    public virtual Permission Permission { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
