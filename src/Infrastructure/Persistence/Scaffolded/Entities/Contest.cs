using System;
using System.Collections.Generic;

Domain.Entities

public partial class Contest
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Slug { get; set; }

    public string? DescriptionMd { get; set; }

    public string VisibilityCode { get; set; } = null!;

    public string? ContestType { get; set; }

    public bool AllowTeams { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public DateTime? FreezeAt { get; set; }

    public DateTime? UnfreezeAt { get; set; }

    public Guid? RemixOfContestId { get; set; }

    public bool IsVirtual { get; set; }

    public string? ConfigJson { get; set; }

    public string? Rules { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public string? InviteCode { get; set; }

    public virtual ICollection<ClassSlot> ClassSlots { get; set; } = new List<ClassSlot>();

    public virtual ICollection<CollectionItem> CollectionItems { get; set; } = new List<CollectionItem>();

    public virtual ICollection<ContestAnalytic> ContestAnalytics { get; set; } = new List<ContestAnalytic>();

    public virtual ICollection<ContestProblem> ContestProblems { get; set; } = new List<ContestProblem>();

    public virtual ICollection<ContestScoreboardEntry> ContestScoreboardEntries { get; set; } = new List<ContestScoreboardEntry>();

    public virtual ICollection<ContestScoreboard> ContestScoreboards { get; set; } = new List<ContestScoreboard>();

    public virtual ICollection<ContestTeam> ContestTeams { get; set; } = new List<ContestTeam>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<Contest> InverseRemixOfContest { get; set; } = new List<Contest>();

    public virtual ICollection<RatingHistory> RatingHistories { get; set; } = new List<RatingHistory>();

    public virtual Contest? RemixOfContest { get; set; }

    public virtual ICollection<ScoreRecalcJob> ScoreRecalcJobs { get; set; } = new List<ScoreRecalcJob>();

    public virtual User? UpdatedByNavigation { get; set; }
}
