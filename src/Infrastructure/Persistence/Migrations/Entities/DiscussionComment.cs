using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("discussion_comments")]
public partial class DiscussionComment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("discussion_id")]
    public Guid DiscussionId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("content")]
    public string Content { get; set; } = null!;

    [Column("parent_id")]
    public Guid? ParentId { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [Column("vote_count")]
    public int VoteCount { get; set; }

    [Column("is_hidden")]
    public bool? IsHidden { get; set; }

    [InverseProperty("Comment")]
    public virtual ICollection<CommentVote> CommentVotes { get; set; } = new List<CommentVote>();

    [ForeignKey("DiscussionId")]
    [InverseProperty("DiscussionComments")]
    public virtual ProblemDiscussion Discussion { get; set; } = null!;

    [InverseProperty("Parent")]
    public virtual ICollection<DiscussionComment> InverseParent { get; set; } = new List<DiscussionComment>();

    [ForeignKey("ParentId")]
    [InverseProperty("InverseParent")]
    public virtual DiscussionComment? Parent { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("DiscussionComments")]
    public virtual User User { get; set; } = null!;
}
