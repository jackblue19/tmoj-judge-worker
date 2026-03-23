using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Migrations.Entities;

[Table("contests")]
[Index("Slug", Name = "contests_slug_key", IsUnique = true)]
public partial class Contest
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("title")]
    public string Title { get; set; } = null!;

    [Column("slug")]
    public string? Slug { get; set; }

    [Column("description_md")]
    public string? DescriptionMd { get; set; }

    [Column("visibility_code")]
    public string VisibilityCode { get; set; } = null!;

    [Column("contest_type")]
    public string? ContestType { get; set; }

    [Column("allow_teams")]
    public bool AllowTeams { get; set; }

    [Column("start_at")]
    public DateTime StartAt { get; set; }

    [Column("end_at")]
    public DateTime EndAt { get; set; }

    [Column("freeze_at")]
    public DateTime? FreezeAt { get; set; }

    [Column("unfreeze_at")]
    public DateTime? UnfreezeAt { get; set; }

    [Column("remix_of_contest_id")]
    public Guid? RemixOfContestId { get; set; }

    [Column("is_virtual")]
    public bool IsVirtual { get; set; }

    [Column("config_json", TypeName = "jsonb")]
    public string? ConfigJson { get; set; }

    [Column("rules")]
    public string? Rules { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    public Guid? UpdatedBy { get; set; }

    [Column("invite_code")]
    public string? InviteCode { get; set; }

    [InverseProperty("Contest")]
    public virtual ICollection<ClassSlot> ClassSlots { get; set; } = new List<ClassSlot>();

    [InverseProperty("Contest")]
    public virtual ICollection<CollectionItem> CollectionItems { get; set; } = new List<CollectionItem>();

    [InverseProperty("Contest")]
    public virtual ICollection<ContestAnalytic> ContestAnalytics { get; set; } = new List<ContestAnalytic>();

    [InverseProperty("Contest")]
    public virtual ICollection<ContestProblem> ContestProblems { get; set; } = new List<ContestProblem>();

    [InverseProperty("Contest")]
    public virtual ICollection<ContestScoreboardEntry> ContestScoreboardEntries { get; set; } = new List<ContestScoreboardEntry>();

    [InverseProperty("Contest")]
    public virtual ICollection<ContestScoreboard> ContestScoreboards { get; set; } = new List<ContestScoreboard>();

    [InverseProperty("Contest")]
    public virtual ICollection<ContestTeam> ContestTeams { get; set; } = new List<ContestTeam>();

    [ForeignKey("CreatedBy")]
    [InverseProperty("ContestCreatedByNavigations")]
    public virtual User? CreatedByNavigation { get; set; }

    [InverseProperty("RemixOfContest")]
    public virtual ICollection<Contest> InverseRemixOfContest { get; set; } = new List<Contest>();

    [InverseProperty("Contest")]
    public virtual ICollection<RatingHistory> RatingHistories { get; set; } = new List<RatingHistory>();

    [ForeignKey("RemixOfContestId")]
    [InverseProperty("InverseRemixOfContest")]
    public virtual Contest? RemixOfContest { get; set; }

    [InverseProperty("Contest")]
    public virtual ICollection<ScoreRecalcJob> ScoreRecalcJobs { get; set; } = new List<ScoreRecalcJob>();

    [ForeignKey("UpdatedBy")]
    [InverseProperty("ContestUpdatedByNavigations")]
    public virtual User? UpdatedByNavigation { get; set; }
}
