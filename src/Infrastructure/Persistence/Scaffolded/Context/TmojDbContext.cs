using System;
using System.Collections.Generic;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Scaffolded.Context;

public partial class TmojDbContext : DbContext
{
    public TmojDbContext()
    {
    }

    public TmojDbContext(DbContextOptions<TmojDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Announcement> Announcements { get; set; }

    public virtual DbSet<ArtifactBlob> ArtifactBlobs { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Badge> Badges { get; set; }

    public virtual DbSet<BadgeRule> BadgeRules { get; set; }

    public virtual DbSet<Checker> Checkers { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassMember> ClassMembers { get; set; }

    public virtual DbSet<ClassSlot> ClassSlots { get; set; }

    public virtual DbSet<ClassSlotProblem> ClassSlotProblems { get; set; }

    public virtual DbSet<CoinConversion> CoinConversions { get; set; }

    public virtual DbSet<Collection> Collections { get; set; }

    public virtual DbSet<CollectionItem> CollectionItems { get; set; }

    public virtual DbSet<CommentVote> CommentVotes { get; set; }

    public virtual DbSet<ContentReport> ContentReports { get; set; }

    public virtual DbSet<Contest> Contests { get; set; }

    public virtual DbSet<ContestAnalytic> ContestAnalytics { get; set; }

    public virtual DbSet<ContestHistory> ContestHistories { get; set; }

    public virtual DbSet<ContestProblem> ContestProblems { get; set; }

    public virtual DbSet<ContestScoreboard> ContestScoreboards { get; set; }

    public virtual DbSet<ContestScoreboardEntry> ContestScoreboardEntries { get; set; }

    public virtual DbSet<ContestTeam> ContestTeams { get; set; }

    public virtual DbSet<DiscussionComment> DiscussionComments { get; set; }

    public virtual DbSet<Editorial> Editorials { get; set; }

    public virtual DbSet<EmailVerification> EmailVerifications { get; set; }

    public virtual DbSet<JudgeJob> JudgeJobs { get; set; }

    public virtual DbSet<JudgeRun> JudgeRuns { get; set; }

    public virtual DbSet<JudgeWorker> JudgeWorkers { get; set; }

    public virtual DbSet<ModerationAction> ModerationActions { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Problem> Problems { get; set; }

    public virtual DbSet<ProblemDiscussion> ProblemDiscussions { get; set; }

    public virtual DbSet<ProblemEditorial> ProblemEditorials { get; set; }

    public virtual DbSet<ProblemStat> ProblemStats { get; set; }

    public virtual DbSet<Provider> Providers { get; set; }

    public virtual DbSet<RatingHistory> RatingHistories { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<ReportsExportHistory> ReportsExportHistories { get; set; }

    public virtual DbSet<Result> Results { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<RunMetric> RunMetrics { get; set; }

    public virtual DbSet<Runtime> Runtimes { get; set; }

    public virtual DbSet<ScoreRecalcJob> ScoreRecalcJobs { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Solution> Solutions { get; set; }

    public virtual DbSet<StorageFile> StorageFiles { get; set; }

    public virtual DbSet<StudyPlan> StudyPlans { get; set; }

    public virtual DbSet<StudyPlanItem> StudyPlanItems { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    public virtual DbSet<Submission> Submissions { get; set; }

    public virtual DbSet<SubmissionQuotum> SubmissionQuota { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<TeamMember> TeamMembers { get; set; }

    public virtual DbSet<Testcase> Testcases { get; set; }

    public virtual DbSet<Testset> Testsets { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserBadge> UserBadges { get; set; }

    public virtual DbSet<UserNotificationSetting> UserNotificationSettings { get; set; }

    public virtual DbSet<UserPermission> UserPermissions { get; set; }

    public virtual DbSet<UserProblemStat> UserProblemStats { get; set; }

    public virtual DbSet<UserProvider> UserProviders { get; set; }

    public virtual DbSet<UserRating> UserRatings { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    public virtual DbSet<UserStreak> UserStreaks { get; set; }

    public virtual DbSet<UserStudyProgress> UserStudyProgresses { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("pgcrypto")
            .HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.AnnouncementId).HasName("announcements_pkey");

            entity.Property(e => e.AnnouncementId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Pinned).HasDefaultValue(false);
            entity.Property(e => e.Target).HasDefaultValueSql("'all'::text");

            entity.HasOne(d => d.Author).WithMany(p => p.Announcements)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("announcements_author_id_fkey");
        });

        modelBuilder.Entity<ArtifactBlob>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("artifact_blobs_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId).HasName("audit_logs_pkey");

            entity.Property(e => e.AuditLogId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ActorType).HasDefaultValueSql("'user'::text");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.ActorUser).WithMany(p => p.AuditLogs)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("audit_logs_actor_user_id_fkey");
        });

        modelBuilder.Entity<Badge>(entity =>
        {
            entity.HasKey(e => e.BadgeId).HasName("badges_pkey");

            entity.Property(e => e.BadgeId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.BadgeLevel).HasDefaultValue(1);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsRepeatable).HasDefaultValue(false);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.BadgeCreatedByNavigations)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("badges_created_by_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.BadgeUpdatedByNavigations)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("badges_updated_by_fkey");
        });

        modelBuilder.Entity<BadgeRule>(entity =>
        {
            entity.HasKey(e => e.BadgeRulesId).HasName("badge_rules_pkey");

            entity.Property(e => e.BadgeRulesId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Badge).WithMany(p => p.BadgeRules).HasConstraintName("badge_rules_badge_id_fkey");
        });

        modelBuilder.Entity<Checker>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("checkers_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.BinaryArtifact).WithMany(p => p.CheckerBinaryArtifacts)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("checkers_binary_artifact_id_fkey");

            entity.HasOne(d => d.Problem).WithMany(p => p.Checkers).HasConstraintName("checkers_problem_id_fkey");

            entity.HasOne(d => d.ProblemTestset).WithMany(p => p.Checkers)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("checkers_problem_testset_id_fkey");

            entity.HasOne(d => d.SourceArtifact).WithMany(p => p.CheckerSourceArtifacts)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("checkers_source_artifact_id_fkey");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("class_pkey");

            entity.Property(e => e.ClassId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Semester).WithMany(p => p.Classes)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("class_semester_id_fkey");

            entity.HasOne(d => d.Subject).WithMany(p => p.Classes)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("class_subject_id_fkey");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Classes)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("class_teacher_id_fkey");
        });

        modelBuilder.Entity<ClassMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("class_member_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.JoinedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassMembers).HasConstraintName("class_member_class_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ClassMembers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("class_member_user_id_fkey");
        });

        modelBuilder.Entity<ClassSlot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("class_slot_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsPublished).HasDefaultValue(false);
            entity.Property(e => e.Mode).HasDefaultValueSql("'problemset'::text");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassSlots).HasConstraintName("class_slot_class_id_fkey");

            entity.HasOne(d => d.Contest).WithMany(p => p.ClassSlots)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("class_slot_contest_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ClassSlotCreatedByNavigations)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("class_slot_created_by_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ClassSlotUpdatedByNavigations)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("class_slot_updated_by_fkey");
        });

        modelBuilder.Entity<ClassSlotProblem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("class_slot_problems_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.IsRequired).HasDefaultValue(true);

            entity.HasOne(d => d.Problem).WithMany(p => p.ClassSlotProblems).HasConstraintName("class_slot_problems_problem_id_fkey");

            entity.HasOne(d => d.Slot).WithMany(p => p.ClassSlotProblems).HasConstraintName("class_slot_problems_slot_id_fkey");
        });

        modelBuilder.Entity<CoinConversion>(entity =>
        {
            entity.HasKey(e => e.ConversionId).HasName("coin_conversion_pkey");

            entity.Property(e => e.ConversionId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Payment).WithMany(p => p.CoinConversions)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("coin_conversion_payment_id_fkey");

            entity.HasOne(d => d.Transaction).WithMany(p => p.CoinConversions)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("coin_conversion_transaction_id_fkey");
        });

        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("collections_pkey");

            entity.HasIndex(e => e.UserId, "uq_user_favorite_contest")
                .IsUnique()
                .HasFilter("((type)::text = 'favorite_contest'::text)");

            entity.HasIndex(e => e.UserId, "uq_user_favorite_problem")
                .IsUnique()
                .HasFilter("((type)::text = 'favorite_problem'::text)");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsVisibility).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithOne(p => p.Collection).HasConstraintName("fk_col_user");
        });

        modelBuilder.Entity<CollectionItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("collection_items_pkey");

            entity.HasIndex(e => new { e.CollectionId, e.ContestId }, "uq_collection_contest")
                .IsUnique()
                .HasFilter("(contest_id IS NOT NULL)");

            entity.HasIndex(e => new { e.CollectionId, e.ProblemId }, "uq_collection_problem")
                .IsUnique()
                .HasFilter("(problem_id IS NOT NULL)");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Collection).WithMany(p => p.CollectionItems).HasConstraintName("fk_ci_collection");

            entity.HasOne(d => d.Contest).WithMany(p => p.CollectionItems)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_ci_contest");

            entity.HasOne(d => d.Problem).WithMany(p => p.CollectionItems)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_ci_problem");
        });

        modelBuilder.Entity<CommentVote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("comment_votes_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Comment).WithMany(p => p.CommentVotes).HasConstraintName("fk_comment_votes_comment");

            entity.HasOne(d => d.User).WithMany(p => p.CommentVotes).HasConstraintName("fk_comment_votes_user");
        });

        modelBuilder.Entity<ContentReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("content_reports_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Status).HasDefaultValueSql("'pending'::character varying");

            entity.HasOne(d => d.Reporter).WithMany(p => p.ContentReports)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("content_reports_reporter_id_fkey");
        });

        modelBuilder.Entity<Contest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("contests_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.AllowTeams).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsVirtual).HasDefaultValue(false);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ContestCreatedByNavigations)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("contests_created_by_fkey");

            entity.HasOne(d => d.RemixOfContest).WithMany(p => p.InverseRemixOfContest)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("contests_remix_of_contest_id_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ContestUpdatedByNavigations)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("contests_updated_by_fkey");
        });

        modelBuilder.Entity<ContestAnalytic>(entity =>
        {
            entity.HasKey(e => new { e.Day, e.ContestId }).HasName("contest_analytics_pkey");

            entity.Property(e => e.AcceptedCount).HasDefaultValue(0);
            entity.Property(e => e.SubmissionsCount).HasDefaultValue(0);
            entity.Property(e => e.UniqueTeams).HasDefaultValue(0);
            entity.Property(e => e.UniqueUsers).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Contest).WithMany(p => p.ContestAnalytics).HasConstraintName("contest_analytics_contest_id_fkey");
        });

        modelBuilder.Entity<ContestHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("contest_history_pkey");

            entity.Property(e => e.HistoryId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ParticipatedAt).HasDefaultValueSql("now()");

            entity.HasMany(d => d.Users).WithMany(p => p.Histories)
                .UsingEntity<Dictionary<string, object>>(
                    "ContestHistoryUser",
                    r => r.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("contest_history_users_user_id_fkey"),
                    l => l.HasOne<ContestHistory>().WithMany()
                        .HasForeignKey("HistoryId")
                        .HasConstraintName("contest_history_users_history_id_fkey"),
                    j =>
                    {
                        j.HasKey("HistoryId", "UserId").HasName("contest_history_users_pkey");
                        j.ToTable("contest_history_users");
                        j.IndexerProperty<Guid>("HistoryId").HasColumnName("history_id");
                        j.IndexerProperty<Guid>("UserId").HasColumnName("user_id");
                    });
        });

        modelBuilder.Entity<ContestProblem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("contest_problems_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Contest).WithMany(p => p.ContestProblems).HasConstraintName("contest_problems_contest_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ContestProblems).HasConstraintName("contest_problems_created_by_fkey");

            entity.HasOne(d => d.OverrideTestset).WithMany(p => p.ContestProblems).HasConstraintName("contest_problems_override_testset_id_fkey");

            entity.HasOne(d => d.Problem).WithMany(p => p.ContestProblems).HasConstraintName("contest_problems_problem_id_fkey");
        });

        modelBuilder.Entity<ContestScoreboard>(entity =>
        {
            entity.HasKey(e => new { e.ContestId, e.EntryId, e.ProblemId }).HasName("contest_scoreboard_pkey");

            entity.Property(e => e.AcmAttempts).HasDefaultValue(0);
            entity.Property(e => e.AcmPenaltyTime).HasDefaultValue(0);
            entity.Property(e => e.AcmSolved).HasDefaultValue(false);

            entity.HasOne(d => d.BestSubmission).WithMany(p => p.ContestScoreboardBestSubmissions)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("contest_scoreboard_best_submission_id_fkey");

            entity.HasOne(d => d.Contest).WithMany(p => p.ContestScoreboards).HasConstraintName("contest_scoreboard_contest_id_fkey");

            entity.HasOne(d => d.Entry).WithMany(p => p.ContestScoreboards).HasConstraintName("contest_scoreboard_entry_id_fkey");

            entity.HasOne(d => d.LastSubmission).WithMany(p => p.ContestScoreboardLastSubmissions)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("contest_scoreboard_last_submission_id_fkey");

            entity.HasOne(d => d.Problem).WithMany(p => p.ContestScoreboards).HasConstraintName("contest_scoreboard_problem_id_fkey");
        });

        modelBuilder.Entity<ContestScoreboardEntry>(entity =>
        {
            entity.HasKey(e => new { e.ContestId, e.EntryId }).HasName("contest_scoreboard_entry_pkey");

            entity.Property(e => e.PenaltyTime).HasDefaultValue(0);
            entity.Property(e => e.SolvedCount).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Contest).WithMany(p => p.ContestScoreboardEntries).HasConstraintName("contest_scoreboard_entry_contest_id_fkey");

            entity.HasOne(d => d.Entry).WithMany(p => p.ContestScoreboardEntries).HasConstraintName("contest_scoreboard_entry_entry_id_fkey");
        });

        modelBuilder.Entity<ContestTeam>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("contest_teams_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.JoinAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Penalty).HasDefaultValue(0);
            entity.Property(e => e.SolvedProblem).HasDefaultValue(0);
            entity.Property(e => e.SubmissionsCount).HasDefaultValue(0);

            entity.HasOne(d => d.Contest).WithMany(p => p.ContestTeams).HasConstraintName("contest_teams_contest_id_fkey");

            entity.HasOne(d => d.Team).WithMany(p => p.ContestTeams).HasConstraintName("contest_teams_team_id_fkey");
        });

        modelBuilder.Entity<DiscussionComment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("discussion_comments_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsHidden).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.VoteCount).HasDefaultValue(0);

            entity.HasOne(d => d.Discussion).WithMany(p => p.DiscussionComments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("discussion_comments_discussion_id_fkey");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent).HasConstraintName("discussion_comments_parent_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.DiscussionComments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("discussion_comments_user_id_fkey");
        });

        modelBuilder.Entity<Editorial>(entity =>
        {
            entity.HasKey(e => e.EditorialId).HasName("editorials_pkey");

            entity.Property(e => e.EditorialId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Author).WithMany(p => p.Editorials)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("editorials_author_id_fkey");

            entity.HasOne(d => d.Storage).WithMany(p => p.Editorials).HasConstraintName("editorials_storage_id_fkey");
        });

        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.HasKey(e => e.VerificationId).HasName("email_verification_pkey");

            entity.Property(e => e.VerificationId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithOne(p => p.EmailVerification).HasConstraintName("email_verification_user_id_fkey");
        });

        modelBuilder.Entity<JudgeJob>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("judge_jobs_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Attempts).HasDefaultValue(0);
            entity.Property(e => e.EnqueueAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Priority).HasDefaultValue(0);
            entity.Property(e => e.TriggerType).HasDefaultValueSql("'submit'::text");

            entity.HasOne(d => d.DequeuedByWorker).WithMany(p => p.JudgeJobs)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("judge_jobs_dequeued_by_worker_id_fkey");

            entity.HasOne(d => d.Submission).WithMany(p => p.JudgeJobs).HasConstraintName("judge_jobs_submission_id_fkey");

            entity.HasOne(d => d.TriggeredByUser).WithMany(p => p.JudgeJobs)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("judge_jobs_triggered_by_user_id_fkey");
        });

        modelBuilder.Entity<JudgeRun>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("judge_runs_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.StartedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.CompileLogBlob).WithMany(p => p.JudgeRuns)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("judge_runs_compile_log_blob_id_fkey");

            entity.HasOne(d => d.Runtime).WithMany(p => p.JudgeRuns)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("judge_runs_runtime_id_fkey");

            entity.HasOne(d => d.Submission).WithMany(p => p.JudgeRuns).HasConstraintName("judge_runs_submission_id_fkey");

            entity.HasOne(d => d.Worker).WithMany(p => p.JudgeRuns)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("judge_runs_worker_id_fkey");
        });

        modelBuilder.Entity<JudgeWorker>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("judge_workers_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        });

        modelBuilder.Entity<ModerationAction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("moderation_actions_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Admin).WithMany(p => p.ModerationActions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("moderation_actions_admin_id_fkey");

            entity.HasOne(d => d.Report).WithMany(p => p.ModerationActions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("moderation_actions_report_id_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("notifications_pkey");

            entity.Property(e => e.NotificationId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsRead).HasDefaultValue(false);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.NotificationCreatedByNavigations)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("notifications_created_by_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.NotificationUsers).HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("payment_pkey");

            entity.Property(e => e.PaymentId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Currency).HasDefaultValueSql("'vnd'::text");
            entity.Property(e => e.Status).HasDefaultValueSql("'pending'::text");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("payment_user_id_fkey");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("permission_pkey");

            entity.Property(e => e.PermissionId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Problem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("problems_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.StatusCode).HasDefaultValueSql("'draft'::text");

            entity.HasOne(d => d.ApprovedByUser).WithMany(p => p.ProblemApprovedByUsers)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("problems_approved_by_user_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ProblemCreatedByNavigations)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("problems_created_by_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ProblemUpdatedByNavigations)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("problems_updated_by_fkey");

            entity.HasMany(d => d.Tags).WithMany(p => p.Problems)
                .UsingEntity<Dictionary<string, object>>(
                    "ProblemTag",
                    r => r.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagId")
                        .HasConstraintName("problem_tags_tag_id_fkey"),
                    l => l.HasOne<Problem>().WithMany()
                        .HasForeignKey("ProblemId")
                        .HasConstraintName("problem_tags_problem_id_fkey"),
                    j =>
                    {
                        j.HasKey("ProblemId", "TagId").HasName("problem_tags_pkey");
                        j.ToTable("problem_tags");
                        j.IndexerProperty<Guid>("ProblemId").HasColumnName("problem_id");
                        j.IndexerProperty<Guid>("TagId").HasColumnName("tag_id");
                    });
        });

        modelBuilder.Entity<ProblemDiscussion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("problem_discussions_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsLocked).HasDefaultValue(false);
            entity.Property(e => e.IsPinned).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Problem).WithMany(p => p.ProblemDiscussions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("problem_discussions_problem_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ProblemDiscussions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("problem_discussions_user_id_fkey");
        });

        modelBuilder.Entity<ProblemEditorial>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("problem_editorials_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Author).WithMany(p => p.ProblemEditorials)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("problem_editorials_author_id_fkey");

            entity.HasOne(d => d.Problem).WithOne(p => p.ProblemEditorial)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("problem_editorials_problem_id_fkey");
        });

        modelBuilder.Entity<ProblemStat>(entity =>
        {
            entity.HasKey(e => e.ProblemId).HasName("problem_stats_pkey");

            entity.Property(e => e.ProblemId).ValueGeneratedNever();
            entity.Property(e => e.AcceptedCount).HasDefaultValue(0L);
            entity.Property(e => e.SubmissionsCount).HasDefaultValue(0L);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Problem).WithOne(p => p.ProblemStat).HasConstraintName("problem_stats_problem_id_fkey");
        });

        modelBuilder.Entity<Provider>(entity =>
        {
            entity.HasKey(e => e.ProviderId).HasName("provider_pkey");

            entity.Property(e => e.ProviderId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Enabled).HasDefaultValue(true);
        });

        modelBuilder.Entity<RatingHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("rating_history_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ProcessedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.ScopeType).HasDefaultValueSql("'global'::text");

            entity.HasOne(d => d.Contest).WithMany(p => p.RatingHistories).HasConstraintName("rating_history_contest_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.RatingHistories).HasConstraintName("rating_history_user_id_fkey");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.TokenId).HasName("refresh_token_pkey");

            entity.Property(e => e.TokenId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.ReplacedByToken).WithMany(p => p.InverseReplacedByToken).HasConstraintName("refresh_token_replaced_by_token_id_fkey");

            entity.HasOne(d => d.Session).WithMany(p => p.RefreshTokens).HasConstraintName("refresh_token_session_id_fkey");
        });

        modelBuilder.Entity<ReportsExportHistory>(entity =>
        {
            entity.HasKey(e => e.ExportId).HasName("reports_export_history_pkey");

            entity.Property(e => e.ExportId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.GeneratedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Status).HasDefaultValueSql("'pending'::text");

            entity.HasOne(d => d.GeneratedByNavigation).WithMany(p => p.ReportsExportHistories)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("reports_export_history_generated_by_fkey");
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("result_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.JudgeRun).WithMany(p => p.Results)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("result_judge_run_id_fkey");

            entity.HasOne(d => d.StderrBlob).WithMany(p => p.ResultStderrBlobs)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("result_stderr_blob_id_fkey");

            entity.HasOne(d => d.StdoutBlob).WithMany(p => p.ResultStdoutBlobs)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("result_stdout_blob_id_fkey");

            entity.HasOne(d => d.Submission).WithMany(p => p.Results).HasConstraintName("result_submission_id_fkey");

            entity.HasOne(d => d.Testcase).WithMany(p => p.Results)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("result_testcase_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("role_pkey");

            entity.Property(e => e.RoleId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsSystem).HasDefaultValue(false);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId }).HasName("role_permission_pkey");

            entity.Property(e => e.Status).HasDefaultValue(true);

            entity.HasOne(d => d.Permission).WithMany(p => p.RolePermissions).HasConstraintName("role_permission_permission_id_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions).HasConstraintName("role_permission_role_id_fkey");
        });

        modelBuilder.Entity<RunMetric>(entity =>
        {
            entity.HasKey(e => e.MetricId).HasName("run_metrics_pkey");

            entity.Property(e => e.MetricId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Runtime>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("runtime_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.DefaultMemoryLimitKb).HasDefaultValue(262144);
            entity.Property(e => e.DefaultTimeLimitMs).HasDefaultValue(1000);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<ScoreRecalcJob>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("score_recalc_jobs_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.EnqueueAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.ContestEntry).WithMany(p => p.ScoreRecalcJobs)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("score_recalc_jobs_contest_entry_id_fkey");

            entity.HasOne(d => d.Contest).WithMany(p => p.ScoreRecalcJobs)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("score_recalc_jobs_contest_id_fkey");

            entity.HasOne(d => d.ContestProblem).WithMany(p => p.ScoreRecalcJobs)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("score_recalc_jobs_contest_problem_id_fkey");
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.SemesterId).HasName("semester_pkey");

            entity.Property(e => e.SemesterId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Solution>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("solution_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasOne(d => d.Problem).WithMany(p => p.Solutions).HasConstraintName("solution_problem_id_fkey");

            entity.HasOne(d => d.Runtime).WithMany(p => p.Solutions).HasConstraintName("solution_runtime_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Solutions).HasConstraintName("solution_user_id_fkey");
        });

        modelBuilder.Entity<StorageFile>(entity =>
        {
            entity.HasKey(e => e.StorageId).HasName("storage_files_pkey");

            entity.Property(e => e.StorageId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsPrivate).HasDefaultValue(true);

            entity.HasOne(d => d.Owner).WithMany(p => p.StorageFiles).HasConstraintName("storage_files_owner_id_fkey");
        });

        modelBuilder.Entity<StudyPlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("study_plans_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsPublic).HasDefaultValue(true);

            entity.HasOne(d => d.Creator).WithMany(p => p.StudyPlans)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("study_plans_creator_id_fkey");
        });

        modelBuilder.Entity<StudyPlanItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("study_plan_items_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.HasOne(d => d.Problem).WithMany(p => p.StudyPlanItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("study_plan_items_problem_id_fkey");

            entity.HasOne(d => d.StudyPlan).WithMany(p => p.StudyPlanItems)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("study_plan_items_study_plan_id_fkey");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId).HasName("subject_pkey");

            entity.Property(e => e.SubjectId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("submissions_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CodeSize).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.StatusCode).HasDefaultValueSql("'queued'::text");
            entity.Property(e => e.SubmissionType).HasDefaultValueSql("'practice'::text");

            entity.HasOne(d => d.CodeArtifact).WithMany(p => p.SubmissionCodeArtifacts)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("submissions_code_artifact_id_fkey");

            entity.HasOne(d => d.ContestProblem).WithMany(p => p.Submissions)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("submissions_contest_problem_id_fkey");

            entity.HasOne(d => d.Problem).WithMany(p => p.Submissions).HasConstraintName("submissions_problem_id_fkey");

            entity.HasOne(d => d.Runtime).WithMany(p => p.Submissions)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("submissions_runtime_id_fkey");

            entity.HasOne(d => d.StorageBlob).WithMany(p => p.SubmissionStorageBlobs)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("submissions_storage_blob_id_fkey");

            entity.HasOne(d => d.Team).WithMany(p => p.Submissions)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("submissions_team_id_fkey");

            entity.HasOne(d => d.Testcase).WithMany(p => p.Submissions)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("submissions_testcase_id_fkey");

            entity.HasOne(d => d.Testset).WithMany(p => p.Submissions)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("submissions_testset_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Submissions).HasConstraintName("submissions_user_id_fkey");
        });

        modelBuilder.Entity<SubmissionQuotum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("submission_quota_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Count).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Problem).WithMany(p => p.SubmissionQuota).HasConstraintName("submission_quota_problem_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.SubmissionQuota).HasConstraintName("submission_quota_user_id_fkey");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tag_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TagCreatedByNavigations)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("tag_created_by_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.TagUpdatedByNavigations)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("tag_updated_by_fkey");
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("team_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsPersonal).HasDefaultValue(false);
            entity.Property(e => e.TeamSize).HasDefaultValue(1);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Leader).WithMany(p => p.Teams).HasConstraintName("team_leader_id_fkey");
        });

        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(e => new { e.TeamId, e.UserId }).HasName("team_members_pkey");

            entity.Property(e => e.JoinedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Team).WithMany(p => p.TeamMembers).HasConstraintName("team_members_team_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.TeamMembers).HasConstraintName("team_members_user_id_fkey");
        });

        modelBuilder.Entity<Testcase>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("testcases_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.IsSample).HasDefaultValue(false);
            entity.Property(e => e.Weight).HasDefaultValue(1);

            entity.HasOne(d => d.StorageBlob).WithMany(p => p.Testcases)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("testcases_storage_blob_id_fkey");

            entity.HasOne(d => d.Testset).WithMany(p => p.Testcases).HasConstraintName("testcases_testset_id_fkey");
        });

        modelBuilder.Entity<Testset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("testsets_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Testsets)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("testsets_created_by_fkey");

            entity.HasOne(d => d.Problem).WithMany(p => p.Testsets).HasConstraintName("testsets_problem_id_fkey");

            entity.HasOne(d => d.StorageBlob).WithMany(p => p.Testsets)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("testsets_storage_blob_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("users_pkey");

            entity.Property(e => e.UserId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.EmailVerified).HasDefaultValue(false);
            entity.Property(e => e.LanguagePreference).HasDefaultValueSql("'en'::text");
            entity.Property(e => e.Status).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Role).WithMany(p => p.Users).HasConstraintName("users_role_id_fkey");
        });

        modelBuilder.Entity<UserBadge>(entity =>
        {
            entity.HasKey(e => e.UserBadgesId).HasName("user_badges_pkey");

            entity.Property(e => e.UserBadgesId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.AwardedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Badge).WithMany(p => p.UserBadges).HasConstraintName("user_badges_badge_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserBadges).HasConstraintName("user_badges_user_id_fkey");
        });

        modelBuilder.Entity<UserNotificationSetting>(entity =>
        {
            entity.HasKey(e => e.SettingId).HasName("user_notification_settings_pkey");

            entity.Property(e => e.SettingId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ReceiveEmail).HasDefaultValue(true);
            entity.Property(e => e.ReceivePush).HasDefaultValue(true);
            entity.Property(e => e.ReceiveSystem).HasDefaultValue(true);

            entity.HasOne(d => d.User).WithOne(p => p.UserNotificationSetting).HasConstraintName("user_notification_settings_user_id_fkey");
        });

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.PermissionId }).HasName("user_permission_pkey");

            entity.Property(e => e.EffectiveAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Status).HasDefaultValue(true);

            entity.HasOne(d => d.Permission).WithMany(p => p.UserPermissions).HasConstraintName("user_permission_permission_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserPermissions).HasConstraintName("user_permission_user_id_fkey");
        });

        modelBuilder.Entity<UserProblemStat>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.ProblemId }).HasName("user_problem_stats_pkey");

            entity.Property(e => e.Attempts).HasDefaultValue(0);
            entity.Property(e => e.Solved).HasDefaultValue(false);

            entity.HasOne(d => d.BestSubmission).WithMany(p => p.UserProblemStats).HasConstraintName("user_problem_stats_best_submission_id_fkey");

            entity.HasOne(d => d.Problem).WithMany(p => p.UserProblemStats).HasConstraintName("user_problem_stats_problem_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserProblemStats).HasConstraintName("user_problem_stats_user_id_fkey");
        });

        modelBuilder.Entity<UserProvider>(entity =>
        {
            entity.HasKey(e => e.UserProviderId).HasName("user_provider_pkey");

            entity.Property(e => e.UserProviderId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Provider).WithMany(p => p.UserProviders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_provider_provider_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserProviders).HasConstraintName("user_provider_user_id_fkey");
        });

        modelBuilder.Entity<UserRating>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_ratings_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.MaxRating).HasDefaultValue(1000);
            entity.Property(e => e.RankTitle).HasDefaultValueSql("'newbie'::text");
            entity.Property(e => e.Rating).HasDefaultValue(1000);
            entity.Property(e => e.ScopeType).HasDefaultValueSql("'global'::text");
            entity.Property(e => e.TimesPlayed).HasDefaultValue(0);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Volatility).HasDefaultValue(0);

            entity.HasOne(d => d.User).WithMany(p => p.UserRatings).HasConstraintName("user_ratings_user_id_fkey");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId }).HasName("user_role_pkey");

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.UserRoleAssignedByNavigations).HasConstraintName("user_role_assigned_by_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles).HasConstraintName("user_role_role_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoleUsers).HasConstraintName("user_role_user_id_fkey");
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("user_session_pkey");

            entity.Property(e => e.SessionId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.LastSeenAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.UserSessions).HasConstraintName("user_session_user_id_fkey");
        });

        modelBuilder.Entity<UserStreak>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_streaks_pkey");

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.CurrentStreak).HasDefaultValue(0);
            entity.Property(e => e.LongestStreak).HasDefaultValue(0);

            entity.HasOne(d => d.User).WithOne(p => p.UserStreak)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_streaks_user_id_fkey");
        });

        modelBuilder.Entity<UserStudyProgress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_study_progress_pkey");

            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.IsCompleted).HasDefaultValue(false);

            entity.HasOne(d => d.Problem).WithMany(p => p.UserStudyProgresses)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_study_progress_problem_id_fkey");

            entity.HasOne(d => d.StudyPlan).WithMany(p => p.UserStudyProgresses)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_study_progress_study_plan_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserStudyProgresses)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_study_progress_user_id_fkey");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("wallet_pkey");

            entity.Property(e => e.WalletId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Currency).HasDefaultValueSql("'coin'::text");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.Wallets)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("wallet_user_id_fkey");
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("wallet_transaction_pkey");

            entity.Property(e => e.TransactionId).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Status).HasDefaultValueSql("'completed'::text");

            entity.HasOne(d => d.Wallet).WithMany(p => p.WalletTransactions)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("wallet_transaction_wallet_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}