using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("badge_rules")]
public partial class BadgeRule
{
    [Key]
    [Column("badge_rules_id")]
    public Guid BadgeRulesId { get; set; }

    [Column("badge_id")]
    public Guid BadgeId { get; set; }

    [Column("rule_type")]
    public string RuleType { get; set; } = null!;

    [Column("target_entity")]
    public string TargetEntity { get; set; } = null!;

    [Column("target_value")]
    public int TargetValue { get; set; }

    [Column("scope_id")]
    public Guid? ScopeId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("BadgeId")]
    [InverseProperty("BadgeRules")]
    public virtual Badge Badge { get; set; } = null!;
}
