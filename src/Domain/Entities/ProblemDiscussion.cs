using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class ProblemDiscussion
{
    public Guid Id { get; set; }

    public Guid ProblemId { get; set; }

    public Guid UserId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public bool? IsPinned { get; set; }

    public bool? IsLocked { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<DiscussionComment> DiscussionComments { get; set; } = new List<DiscussionComment>();

    public virtual Problem Problem { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
