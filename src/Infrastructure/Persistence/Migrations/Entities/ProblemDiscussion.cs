using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("problem_discussions")]
public partial class ProblemDiscussion
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("problem_id")]
    public Guid ProblemId { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("title")]
    public string Title { get; set; } = null!;

    [Column("content")]
    public string Content { get; set; } = null!;

    [Column("is_pinned")]
    public bool? IsPinned { get; set; }

    [Column("is_locked")]
    public bool? IsLocked { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Discussion")]
    public virtual ICollection<DiscussionComment> DiscussionComments { get; set; } = new List<DiscussionComment>();

    [ForeignKey("ProblemId")]
    [InverseProperty("ProblemDiscussions")]
    public virtual Problem Problem { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("ProblemDiscussions")]
    public virtual User User { get; set; } = null!;
}
