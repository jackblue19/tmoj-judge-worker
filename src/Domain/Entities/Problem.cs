using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("problems")]
[Index("Slug", Name = "problems_slug_key", IsUnique = true)]
public partial class Problem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("slug")]
    public string? Slug { get; set; }

    [Column("title")]
    public string Title { get; set; } = null!;

    [Column("difficulty")]
    public string? Difficulty { get; set; }

    [Column("type_code")]
    public string? TypeCode { get; set; }

    [Column("visibility_code")]
    public string? VisibilityCode { get; set; }

    [Column("scoring_code")]
    public string? ScoringCode { get; set; }

    [Column("status_code")]
    public string StatusCode { get; set; } = null!;

    [Column("approved_by_user_id")]
    public Guid? ApprovedByUserId { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("published_at")]
    public DateTime? PublishedAt { get; set; }

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

    [Column("description_md")]
    public string? DescriptionMd { get; set; }

    [Column("acceptance_percent")]
    [Precision(5, 2)]
    public decimal? AcceptancePercent { get; set; }

    [Column("display_index")]
    public int? DisplayIndex { get; set; }

    [Column("time_limit_ms")]
    public int? TimeLimitMs { get; set; }

    [Column("memory_limit_kb")]
    public int? MemoryLimitKb { get; set; }

    [ForeignKey("ApprovedByUserId")]
    [InverseProperty("ProblemApprovedByUsers")]
    public virtual User? ApprovedByUser { get; set; }

    [InverseProperty("Problem")]
    public virtual ICollection<Checker> Checkers { get; set; } = new List<Checker>();

    [InverseProperty("Problem")]
    public virtual ICollection<ClassSlotProblem> ClassSlotProblems { get; set; } = new List<ClassSlotProblem>();

    [InverseProperty("Problem")]
    public virtual ICollection<CollectionItem> CollectionItems { get; set; } = new List<CollectionItem>();

    [InverseProperty("Problem")]
    public virtual ICollection<ContestProblem> ContestProblems { get; set; } = new List<ContestProblem>();

    [InverseProperty("Problem")]
    public virtual ICollection<ContestScoreboard> ContestScoreboards { get; set; } = new List<ContestScoreboard>();

    [ForeignKey("CreatedBy")]
    [InverseProperty("ProblemCreatedByNavigations")]
    public virtual User? CreatedByNavigation { get; set; }

    [InverseProperty("Problem")]
    public virtual ICollection<ProblemDiscussion> ProblemDiscussions { get; set; } = new List<ProblemDiscussion>();

    [InverseProperty("Problem")]
    public virtual ProblemEditorial? ProblemEditorial { get; set; }

    [InverseProperty("Problem")]
    public virtual ProblemStat? ProblemStat { get; set; }

    [InverseProperty("Problem")]
    public virtual ICollection<Solution> Solutions { get; set; } = new List<Solution>();

    [InverseProperty("Problem")]
    public virtual ICollection<StudyPlanItem> StudyPlanItems { get; set; } = new List<StudyPlanItem>();

    [InverseProperty("Problem")]
    public virtual ICollection<SubmissionQuotum> SubmissionQuota { get; set; } = new List<SubmissionQuotum>();

    [InverseProperty("Problem")]
    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    [InverseProperty("Problem")]
    public virtual ICollection<Testset> Testsets { get; set; } = new List<Testset>();

    [ForeignKey("UpdatedBy")]
    [InverseProperty("ProblemUpdatedByNavigations")]
    public virtual User? UpdatedByNavigation { get; set; }

    [InverseProperty("Problem")]
    public virtual ICollection<UserProblemStat> UserProblemStats { get; set; } = new List<UserProblemStat>();

    [InverseProperty("Problem")]
    public virtual ICollection<UserStudyProgress> UserStudyProgresses { get; set; } = new List<UserStudyProgress>();

    [ForeignKey("ProblemId")]
    [InverseProperty("Problems")]
    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
