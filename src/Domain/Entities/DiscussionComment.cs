using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class DiscussionComment
{
    public Guid Id { get; set; }

    public Guid DiscussionId { get; set; }

    public Guid UserId { get; set; }

    public string Content { get; set; } = null!;

    public Guid? ParentId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int VoteCount { get; set; }

    public bool? IsHidden { get; set; }



    public virtual ProblemDiscussion Discussion { get; set; } = null!;

    public virtual ICollection<DiscussionComment> InverseParent { get; set; } = new List<DiscussionComment>();

    public virtual DiscussionComment? Parent { get; set; }

    public virtual User User { get; set; } = null!;
}
