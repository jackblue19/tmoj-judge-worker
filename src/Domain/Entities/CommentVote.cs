using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("comment_votes")]
[Index("CommentId", Name = "idx_comment_votes_comment")]
[Index("UserId", Name = "idx_comment_votes_user")]
[Index("UserId", "CommentId", Name = "uq_user_comment_vote", IsUnique = true)]
public partial class CommentVote
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("comment_id")]
    public Guid CommentId { get; set; }

    [Column("vote")]
    public short Vote { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("CommentId")]
    [InverseProperty("CommentVotes")]
    public virtual DiscussionComment Comment { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("CommentVotes")]
    public virtual User User { get; set; } = null!;
}
