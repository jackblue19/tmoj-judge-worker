using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class BadgeRule
{
    public Guid BadgeRulesId { get; set; }

    public Guid BadgeId { get; set; }

    public string RuleType { get; set; } = null!;

    public string TargetEntity { get; set; } = null!;

    public int TargetValue { get; set; }

    public Guid? ScopeId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Badge Badge { get; set; } = null!;
}
