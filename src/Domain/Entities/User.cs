using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities;

[Table("users")]
[Index("Email", Name = "users_email_key", IsUnique = true)]
[Index("Username", Name = "users_username_key", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; } = null!;

    [Column("last_name")]
    public string LastName { get; set; } = null!;

    [Column("username")]
    public string Username { get; set; } = null!;

    [Column("roll_number")]
    public string? RollNumber { get; set; }

    [Column("member_code")]
    public string? MemberCode { get; set; }

    [Column("email")]
    public string Email { get; set; } = null!;

    [Column("email_verified")]
    public bool EmailVerified { get; set; }

    [Column("password")]
    public string? Password { get; set; }

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("display_name")]
    public string? DisplayName { get; set; }

    [Column("language_preference")]
    public string LanguagePreference { get; set; } = null!;

    [Column("status")]
    public bool Status { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("role_id")]
    public Guid? RoleId { get; set; }

    [Column("roll_number")]
    [StringLength(255)]
    public string? RollNumber { get; set; }

    [Column("member_code")]
    [StringLength(255)]
    public string? MemberCode { get; set; }

    [InverseProperty("Author")]
    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();

    [InverseProperty("ActorUser")]
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Badge> BadgeCreatedByNavigations { get; set; } = new List<Badge>();

    [InverseProperty("UpdatedByNavigation")]
    public virtual ICollection<Badge> BadgeUpdatedByNavigations { get; set; } = new List<Badge>();

    [InverseProperty("User")]
    public virtual ICollection<ClassMember> ClassMembers { get; set; } = new List<ClassMember>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<ClassSlot> ClassSlotCreatedByNavigations { get; set; } = new List<ClassSlot>();

    [InverseProperty("UpdatedByNavigation")]
    public virtual ICollection<ClassSlot> ClassSlotUpdatedByNavigations { get; set; } = new List<ClassSlot>();

    [InverseProperty("Teacher")]
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    [InverseProperty("User")]
    public virtual Collection? Collection { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<CommentVote> CommentVotes { get; set; } = new List<CommentVote>();

    [InverseProperty("Reporter")]
    public virtual ICollection<ContentReport> ContentReports { get; set; } = new List<ContentReport>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Contest> ContestCreatedByNavigations { get; set; } = new List<Contest>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<ContestProblem> ContestProblems { get; set; } = new List<ContestProblem>();

    [InverseProperty("UpdatedByNavigation")]
    public virtual ICollection<Contest> ContestUpdatedByNavigations { get; set; } = new List<Contest>();

    [InverseProperty("User")]
    public virtual ICollection<DiscussionComment> DiscussionComments { get; set; } = new List<DiscussionComment>();

    [InverseProperty("Author")]
    public virtual ICollection<Editorial> Editorials { get; set; } = new List<Editorial>();

    [InverseProperty("User")]
    public virtual EmailVerification? EmailVerification { get; set; }

    [InverseProperty("TriggeredByUser")]
    public virtual ICollection<JudgeJob> JudgeJobs { get; set; } = new List<JudgeJob>();

    [InverseProperty("Admin")]
    public virtual ICollection<ModerationAction> ModerationActions { get; set; } = new List<ModerationAction>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Notification> NotificationCreatedByNavigations { get; set; } = new List<Notification>();

    [InverseProperty("User")]
    public virtual ICollection<Notification> NotificationUsers { get; set; } = new List<Notification>();

    [InverseProperty("User")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [InverseProperty("ApprovedByUser")]
    public virtual ICollection<Problem> ProblemApprovedByUsers { get; set; } = new List<Problem>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Problem> ProblemCreatedByNavigations { get; set; } = new List<Problem>();

    [InverseProperty("User")]
    public virtual ICollection<ProblemDiscussion> ProblemDiscussions { get; set; } = new List<ProblemDiscussion>();

    [InverseProperty("Author")]
    public virtual ICollection<ProblemEditorial> ProblemEditorials { get; set; } = new List<ProblemEditorial>();

    [InverseProperty("UpdatedByNavigation")]
    public virtual ICollection<Problem> ProblemUpdatedByNavigations { get; set; } = new List<Problem>();

    [InverseProperty("User")]
    public virtual ICollection<RatingHistory> RatingHistories { get; set; } = new List<RatingHistory>();

    [InverseProperty("GeneratedByNavigation")]
    public virtual ICollection<ReportsExportHistory> ReportsExportHistories { get; set; } = new List<ReportsExportHistory>();

    [ForeignKey("RoleId")]
    [InverseProperty("Users")]
    public virtual Role? Role { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<Solution> Solutions { get; set; } = new List<Solution>();

    [InverseProperty("Owner")]
    public virtual ICollection<StorageFile> StorageFiles { get; set; } = new List<StorageFile>();

    [InverseProperty("Creator")]
    public virtual ICollection<StudyPlan> StudyPlans { get; set; } = new List<StudyPlan>();

    [InverseProperty("User")]
    public virtual ICollection<SubmissionQuotum> SubmissionQuota { get; set; } = new List<SubmissionQuotum>();

    [InverseProperty("User")]
    public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Tag> TagCreatedByNavigations { get; set; } = new List<Tag>();

    [InverseProperty("UpdatedByNavigation")]
    public virtual ICollection<Tag> TagUpdatedByNavigations { get; set; } = new List<Tag>();

    [InverseProperty("User")]
    public virtual ICollection<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();

    [InverseProperty("Leader")]
    public virtual ICollection<Team> Teams { get; set; } = new List<Team>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Testset> Testsets { get; set; } = new List<Testset>();

    [InverseProperty("User")]
    public virtual ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();

    [InverseProperty("User")]
    public virtual UserNotificationSetting? UserNotificationSetting { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();

    [InverseProperty("User")]
    public virtual ICollection<UserProblemStat> UserProblemStats { get; set; } = new List<UserProblemStat>();

    [InverseProperty("User")]
    public virtual ICollection<UserProvider> UserProviders { get; set; } = new List<UserProvider>();

    [InverseProperty("User")]
    public virtual ICollection<UserRating> UserRatings { get; set; } = new List<UserRating>();

    [InverseProperty("AssignedByNavigation")]
    public virtual ICollection<UserRole> UserRoleAssignedByNavigations { get; set; } = new List<UserRole>();

    [InverseProperty("User")]
    public virtual ICollection<UserRole> UserRoleUsers { get; set; } = new List<UserRole>();

    [InverseProperty("User")]
    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();

    [InverseProperty("User")]
    public virtual UserStreak? UserStreak { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<UserStudyProgress> UserStudyProgresses { get; set; } = new List<UserStudyProgress>();

    [InverseProperty("User")]
    public virtual ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();

    [ForeignKey("UserId")]
    [InverseProperty("Users")]
    public virtual ICollection<ContestHistory> Histories { get; set; } = new List<ContestHistory>();
}
