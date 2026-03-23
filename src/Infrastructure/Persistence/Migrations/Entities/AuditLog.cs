using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("audit_logs")]
public partial class AuditLog
{
    [Key]
    [Column("audit_log_id")]
    public Guid AuditLogId { get; set; }

    [Column("actor_user_id")]
    public Guid? ActorUserId { get; set; }

    [Column("actor_type")]
    public string ActorType { get; set; } = null!;

    [Column("action_code")]
    public string ActionCode { get; set; } = null!;

    [Column("action_category")]
    public string ActionCategory { get; set; } = null!;

    [Column("target_table")]
    public string? TargetTable { get; set; }

    [Column("target_pk")]
    public string? TargetPk { get; set; }

    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    [Column("trace_id")]
    public Guid? TraceId { get; set; }

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ActorUserId")]
    [InverseProperty("AuditLogs")]
    public virtual User? ActorUser { get; set; }
}
