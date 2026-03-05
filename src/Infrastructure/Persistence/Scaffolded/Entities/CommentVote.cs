using System;
using System.Collections.Generic;

Domain.Entities

public partial class CommentVote
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CommentId { get; set; }

    public short Vote { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual DiscussionComment Comment { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
