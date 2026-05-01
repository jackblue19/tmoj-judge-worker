using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ContentVote
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid TargetId { get; set; }
    
    public string TargetType { get; set; } = null!;

    public short Vote { get; set; }

    public DateTime CreatedAt { get; set; }
}
