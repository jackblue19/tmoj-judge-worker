using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ContentReport
{
    public Guid Id { get; set; }

    public Guid ReporterId { get; set; }

    public string TargetType { get; set; } = null!;

    public Guid TargetId { get; set; }

    public string Reason { get; set; } = null!;

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ModerationAction> ModerationActions { get; set; } = new List<ModerationAction>();

    public virtual User Reporter { get; set; } = null!;
}
