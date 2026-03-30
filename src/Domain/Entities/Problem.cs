using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class Problem
{
    public Guid Id { get; set; }

    public string? Slug { get; set; }

    public string Title { get; set; } = null!;

    public string? Difficulty { get; set; }

    public string? TypeCode { get; set; }

    public string? VisibilityCode { get; set; }

    public string? ScoringCode { get; set; }

    public string StatusCode { get; set; } = null!;

    public Guid? ApprovedByUserId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? PublishedAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Guid? UpdatedBy { get; set; }

    public string? DescriptionMd { get; set; }

    public decimal? AcceptancePercent { get; set; }

    public int? DisplayIndex { get; set; }

    public int? TimeLimitMs { get; set; }

    public int? MemoryLimitKb { get; set; }

    public string? StatementSourceCode { get; set; }

    public Guid? StatementFileId { get; set; }

    public string? StatementFileName { get; set; }

    public string? StatementContentType { get; set; }

    public string? StatementExtension { get; set; }

    public DateTime? StatementUploadedAt { get; set; }

    public virtual User? ApprovedByUser { get; set; }

    public virtual ICollection<Checker> Checkers { get; set; } = new List<Checker>();

    public virtual ICollection<ClassSlotProblem> ClassSlotProblems { get; set; } = new List<ClassSlotProblem>();

    public virtual ICollection<CollectionItem> CollectionItems { get; set; } = new List<CollectionItem>();

    public virtual ICollection<ContestProblem> ContestProblems { get; set; } = new List<ContestProblem>();

    public virtual ICollection<ContestScoreboard> ContestScoreboards { get; set; } = new List<ContestScoreboard>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<ProblemDiscussion> ProblemDiscussions { get; set; } = new List<ProblemDiscussion>();

    public virtual ProblemEditorial? ProblemEditorial { get; set; }

    public virtual ProblemStat? ProblemStat { get; set; }

    public virtual ICollection<Solution> Solutions { get; set; } = new List<Solution>();

    public virtual ICollection<StudyPlanItem> StudyPlanItems { get; set; } = new List<StudyPlanItem>();

    public virtual ICollection<SubmissionQuotum> SubmissionQuota { get; set; } = new List<SubmissionQuotum>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    public virtual ICollection<Testset> Testsets { get; set; } = new List<Testset>();

    public virtual User? UpdatedByNavigation { get; set; }

    public virtual ICollection<UserProblemStat> UserProblemStats { get; set; } = new List<UserProblemStat>();

    public virtual ICollection<UserStudyProgress> UserStudyProgresses { get; set; } = new List<UserStudyProgress>();

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
