using System;
using System.Collections.Generic;

namespace Domain.Entities;

public partial class User
{
    public Guid UserId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public bool EmailVerified { get; set; }

    public string? Password { get; set; }

    public string? AvatarUrl { get; set; }

    public string? DisplayName { get; set; }

    public string LanguagePreference { get; set; } = null!;

    public bool Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    public Guid? RoleId { get; set; }

    public string? RollNumber { get; set; }

    public string? MemberCode { get; set; }

    //public bool IsBanned { get; set; }

    //public DateTime? BannedUntil { get; set; }

    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<Badge> BadgeCreatedByNavigations { get; set; } = new List<Badge>();

    public virtual ICollection<Badge> BadgeUpdatedByNavigations { get; set; } = new List<Badge>();

    public virtual ICollection<ClassMember> ClassMembers { get; set; } = new List<ClassMember>();

    public virtual ICollection<ClassSemester> ClassSemesters { get; set; } = new List<ClassSemester>();

    public virtual ICollection<ClassSlot> ClassSlotCreatedByNavigations { get; set; } = new List<ClassSlot>();

    public virtual ICollection<ClassSlot> ClassSlotUpdatedByNavigations { get; set; } = new List<ClassSlot>();

    public virtual Collection? Collection { get; set; }

    public virtual ICollection<CommentVote> CommentVotes { get; set; } = new List<CommentVote>();

    public virtual ICollection<ContentReport> ContentReports { get; set; } = new List<ContentReport>();

    public virtual ICollection<Contest> ContestCreatedByNavigations { get; set; } = new List<Contest>();

    public virtual ICollection<ContestProblem> ContestProblems { get; set; } = new List<ContestProblem>();

    public virtual ICollection<Contest> ContestUpdatedByNavigations { get; set; } = new List<Contest>();

    public virtual ICollection<DiscussionComment> DiscussionComments { get; set; } = new List<DiscussionComment>();

    public virtual ICollection<Editorial> Editorials { get; set; } = new List<Editorial>();

    public virtual EmailVerification? EmailVerification { get; set; }

    public virtual ICollection<JudgeJob> JudgeJobs { get; set; } = new List<JudgeJob>();

    public virtual ICollection<ModerationAction> ModerationActions { get; set; } = new List<ModerationAction>();

    public virtual ICollection<Notification> NotificationCreatedByNavigations { get; set; } = new List<Notification>();

    public virtual ICollection<Notification> NotificationUsers { get; set; } = new List<Notification>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Problem> ProblemApprovedByUsers { get; set; } = new List<Problem>();

    public virtual ICollection<Problem> ProblemCreatedByNavigations { get; set; } = new List<Problem>();

    public virtual ICollection<ProblemDiscussion> ProblemDiscussions { get; set; } = new List<ProblemDiscussion>();

    public virtual ICollection<ProblemEditorial> ProblemEditorials { get; set; } = new List<ProblemEditorial>();

    public virtual ICollection<Problem> ProblemUpdatedByNavigations { get; set; } = new List<Problem>();

    public virtual ICollection<RatingHistory> RatingHistories { get; set; } = new List<RatingHistory>();

    public virtual ICollection<ReportsExportHistory> ReportsExportHistories { get; set; } = new List<ReportsExportHistory>();

    public virtual Role? Role { get; set; }

    public virtual ICollection<Solution> Solutions { get; set; } = new List<Solution>();

    public virtual ICollection<StorageFile> StorageFiles { get; set; } = new List<StorageFile>();

    public virtual ICollection<StudyPlan> StudyPlans { get; set; } = new List<StudyPlan>();

    public virtual ICollection<SubmissionQuotum> SubmissionQuota { get; set; } = new List<SubmissionQuotum>();

    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    public virtual ICollection<Tag> TagCreatedByNavigations { get; set; } = new List<Tag>();

    public virtual ICollection<Tag> TagUpdatedByNavigations { get; set; } = new List<Tag>();

    public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();

    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();

    public virtual ICollection<Testset> Testsets { get; set; } = new List<Testset>();

    public virtual ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();

    public virtual UserNotificationSetting? UserNotificationSetting { get; set; }

    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();

    public virtual ICollection<UserProblemStat> UserProblemStats { get; set; } = new List<UserProblemStat>();

    public virtual ICollection<UserProvider> UserProviders { get; set; } = new List<UserProvider>();

    public virtual ICollection<UserRating> UserRatings { get; set; } = new List<UserRating>();

    public virtual ICollection<UserRole> UserRoleAssignedByNavigations { get; set; } = new List<UserRole>();

    public virtual ICollection<UserRole> UserRoleUsers { get; set; } = new List<UserRole>();

    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();

    public virtual UserStreak? UserStreak { get; set; }

    public virtual ICollection<UserStudyProgress> UserStudyProgresses { get; set; } = new List<UserStudyProgress>();

    public virtual ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();

    public virtual ICollection<ContestHistory> Histories { get; set; } = new List<ContestHistory>();

    public virtual ICollection<UserStudyPlanPurchase> UserStudyPlanPurchases { get; set; }
     = new List<UserStudyPlanPurchase>();

    public virtual ICollection<UserStudyItemProgress> UserStudyItemProgresses { get; set; }
    = new List<UserStudyItemProgress>();

    public virtual ICollection<UserInventory> UserInventories { get; set; } = new List<UserInventory>();
}
