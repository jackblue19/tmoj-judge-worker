using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ModerationAction
{
    public Guid Id { get; set; }

    public Guid ReportId { get; set; }

    public Guid AdminId { get; set; }

    public string ActionType { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User Admin { get; set; } = null!;

    public virtual ContentReport Report { get; set; } = null!;
}
