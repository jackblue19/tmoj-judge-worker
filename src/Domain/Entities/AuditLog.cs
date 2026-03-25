using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class AuditLog
{
    public Guid AuditLogId { get; set; }

    public Guid? ActorUserId { get; set; }

    public string ActorType { get; set; } = null!;

    public string ActionCode { get; set; } = null!;

    public string ActionCategory { get; set; } = null!;

    public string? TargetTable { get; set; }

    public string? TargetPk { get; set; }

    public string? Metadata { get; set; }

    public Guid? TraceId { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? ActorUser { get; set; }
}
