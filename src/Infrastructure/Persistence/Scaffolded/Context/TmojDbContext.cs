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

            entity.ToTable("announcements");

            entity.Property(e => e.AnnouncementId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("announcement_id");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Pinned)
                .HasDefaultValue(false)
                .HasColumnName("pinned");
            entity.Property(e => e.ScopeId).HasColumnName("scope_id");
            entity.Property(e => e.ScopeType).HasColumnName("scope_type");
            entity.Property(e => e.Target)
                .HasDefaultValueSql("'all'::text")
                .HasColumnName("target");
            entity.Property(e => e.Title).HasColumnName("title");

            entity.HasOne(d => d.Author).WithMany(p => p.Announcements)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("announcements_author_id_fkey");
        });

        modelBuilder.Entity<ArtifactBlob>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("artifact_blobs_pkey");

            entity.ToTable("artifact_blobs");

            entity.HasIndex(e => new { e.Sha256, e.SizeBytes }, "artifact_blobs_sha256_size_bytes_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ContentType).HasColumnName("content_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Sha256).HasColumnName("sha256");
            entity.Property(e => e.SizeBytes).HasColumnName("size_bytes");
            entity.Property(e => e.StorageUri).HasColumnName("storage_uri");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditLogId).HasName("audit_logs_pkey");

            entity.ToTable("audit_logs");

            entity.Property(e => e.AuditLogId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("audit_log_id");
            entity.Property(e => e.ActionCategory).HasColumnName("action_category");
            entity.Property(e => e.ActionCode).HasColumnName("action_code");
            entity.Property(e => e.ActorType)
                .HasDefaultValueSql("'user'::text")
                .HasColumnName("actor_type");
            entity.Property(e => e.ActorUserId).HasColumnName("actor_user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.TargetPk).HasColumnName("target_pk");
            entity.Property(e => e.TargetTable).HasColumnName("target_table");
            entity.Property(e => e.TraceId).HasColumnName("trace_id");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");

            entity.HasOne(d => d.ActorUser).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("audit_logs_actor_user_id_fkey");
        });

        modelBuilder.Entity<Badge>(entity =>
        {
            entity.HasKey(e => e.BadgeId).HasName("badges_pkey");

            entity.ToTable("badges");

            entity.HasIndex(e => e.BadgeCode, "badges_badge_code_key").IsUnique();

            entity.Property(e => e.BadgeId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("badge_id");
            entity.Property(e => e.BadgeCategory).HasColumnName("badge_category");
            entity.Property(e => e.BadgeCode).HasColumnName("badge_code");
            entity.Property(e => e.BadgeLevel)
                .HasDefaultValue(1)
                .HasColumnName("badge_level");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IconUrl).HasColumnName("icon_url");
            entity.Property(e => e.IsRepeatable)
                .HasDefaultValue(false)
                .HasColumnName("is_repeatable");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.BadgeCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("badges_created_by_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.BadgeUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("badges_updated_by_fkey");
        });

        modelBuilder.Entity<BadgeRule>(entity =>
        {
            entity.HasKey(e => e.BadgeRulesId).HasName("badge_rules_pkey");

            entity.ToTable("badge_rules");

            entity.Property(e => e.BadgeRulesId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("badge_rules_id");
            entity.Property(e => e.BadgeId).HasColumnName("badge_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.RuleType).HasColumnName("rule_type");
            entity.Property(e => e.ScopeId).HasColumnName("scope_id");
            entity.Property(e => e.TargetEntity).HasColumnName("target_entity");
            entity.Property(e => e.TargetValue).HasColumnName("target_value");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Badge).WithMany(p => p.BadgeRules)
                .HasForeignKey(d => d.BadgeId)
                .HasConstraintName("badge_rules_badge_id_fkey");
        });

        modelBuilder.Entity<Checker>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("checkers_pkey");

            entity.ToTable("checkers");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.BinaryArtifactId).HasColumnName("binary_artifact_id");
            entity.Property(e => e.Entrypoint).HasColumnName("entrypoint");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.ProblemTestsetId).HasColumnName("problem_testset_id");
            entity.Property(e => e.SourceArtifactId).HasColumnName("source_artifact_id");
            entity.Property(e => e.Type).HasColumnName("type");

            entity.HasOne(d => d.BinaryArtifact).WithMany(p => p.CheckerBinaryArtifacts)
                .HasForeignKey(d => d.BinaryArtifactId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("checkers_binary_artifact_id_fkey");

            entity.HasOne(d => d.Problem).WithMany(p => p.Checkers)
                .HasForeignKey(d => d.ProblemId)
                .HasConstraintName("checkers_problem_id_fkey");

            entity.HasOne(d => d.ProblemTestset).WithMany(p => p.Checkers)
                .HasForeignKey(d => d.ProblemTestsetId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("checkers_problem_testset_id_fkey");

            entity.HasOne(d => d.SourceArtifact).WithMany(p => p.CheckerSourceArtifacts)
                .HasForeignKey(d => d.SourceArtifactId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("checkers_source_artifact_id_fkey");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("class_pkey");

            entity.ToTable("class");

            entity.HasIndex(e => e.ClassCode, "class_class_code_key").IsUnique();

            entity.HasIndex(e => e.InviteCode, "class_invite_code_key").IsUnique();

            entity.Property(e => e.ClassId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("class_id");
            entity.Property(e => e.ClassCode).HasColumnName("class_code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.InviteCode).HasColumnName("invite_code");
            entity.Property(e => e.InviteCodeExpiresAt).HasColumnName("invite_code_expires_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.SemesterId).HasColumnName("semester_id");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.SubjectId).HasColumnName("subject_id");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.InviteCode).HasColumnName("invite_code");

            entity.HasOne(d => d.Semester).WithMany(p => p.Classes)
                .HasForeignKey(d => d.SemesterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("class_semester_id_fkey");

            entity.HasOne(d => d.Subject).WithMany(p => p.Classes)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("class_subject_id_fkey");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Classes)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("class_teacher_id_fkey");
        });

        modelBuilder.Entity<ClassMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("class_member_pkey");

            entity.ToTable("class_member");

            entity.HasIndex(e => new { e.ClassId, e.UserId }, "uq_class_user").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("joined_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassMembers)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("class_member_class_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ClassMembers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("class_member_user_id_fkey");
        });

        modelBuilder.Entity<ClassSlot>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("class_slot_pkey");

            entity.ToTable("class_slot");

            entity.HasIndex(e => new { e.ClassId, e.SlotNo }, "ux_class_slot").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ClassId).HasColumnName("class_id");
            entity.Property(e => e.CloseAt).HasColumnName("close_at");
            entity.Property(e => e.ContestId).HasColumnName("contest_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DueAt).HasColumnName("due_at");
            entity.Property(e => e.IsPublished)
                .HasDefaultValue(false)
                .HasColumnName("is_published");
            entity.Property(e => e.Mode)
                .HasDefaultValueSql("'problemset'::text")
                .HasColumnName("mode");
            entity.Property(e => e.OpenAt).HasColumnName("open_at");
            entity.Property(e => e.Rules).HasColumnName("rules");
            entity.Property(e => e.SlotNo).HasColumnName("slot_no");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.Class).WithMany(p => p.ClassSlots)
                .HasForeignKey(d => d.ClassId)
                .HasConstraintName("class_slot_class_id_fkey");

            entity.HasOne(d => d.Contest).WithMany(p => p.ClassSlots)
                .HasForeignKey(d => d.ContestId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("class_slot_contest_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ClassSlotCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("class_slot_created_by_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ClassSlotUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("class_slot_updated_by_fkey");
        });

        modelBuilder.Entity<ClassSlotProblem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("class_slot_problems_pkey");

            entity.ToTable("class_slot_problems");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");

            entity.Property(e => e.SlotId).HasColumnName("slot_id");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.IsRequired)
                .HasDefaultValue(true)
                .HasColumnName("is_required");
            entity.Property(e => e.Ordinal).HasColumnName("ordinal");
            entity.Property(e => e.Points).HasColumnName("points");

            // Add unique index for (SlotId, ProblemId) to maintain integrity
            entity.HasIndex(e => new { e.SlotId, e.ProblemId }, "uq_slot_problem").IsUnique();

            entity.HasOne(d => d.Problem).WithMany(p => p.ClassSlotProblems)
                .HasForeignKey(d => d.ProblemId)
                .HasConstraintName("class_slot_problems_problem_id_fkey");

            entity.HasOne(d => d.Slot).WithMany(p => p.ClassSlotProblems)
                .HasForeignKey(d => d.SlotId)
                .HasConstraintName("class_slot_problems_slot_id_fkey");
        });

        modelBuilder.Entity<CoinConversion>(entity =>
        {
            entity.HasKey(e => e.ConversionId).HasName("coin_conversion_pkey");

            entity.ToTable("coin_conversion");

            entity.Property(e => e.ConversionId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("conversion_id");
            entity.Property(e => e.CoinAmount)
                .HasPrecision(18, 2)
                .HasColumnName("coin_amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Rate)
                .HasPrecision(18, 6)
                .HasColumnName("rate");
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id");

            entity.HasOne(d => d.Payment).WithMany(p => p.CoinConversions)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("coin_conversion_payment_id_fkey");

            entity.HasOne(d => d.Transaction).WithMany(p => p.CoinConversions)
                .HasForeignKey(d => d.TransactionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("coin_conversion_transaction_id_fkey");
        });

        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("collections_pkey");

            entity.ToTable("collections");

            entity.HasIndex(e => e.UserId, "uq_user_favorite_contest")
                .IsUnique()
                .HasFilter("((type)::text = 'favorite_contest'::text)");

            entity.HasIndex(e => e.UserId, "uq_user_favorite_problem")
                .IsUnique()
                .HasFilter("((type)::text = 'favorite_problem'::text)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsVisibility)
                .HasDefaultValue(false)
                .HasColumnName("is_visibility");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Type)
                .HasMaxLength(30)
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Collection)
                .HasForeignKey<Collection>(d => d.UserId)
                .HasConstraintName("fk_col_user");
        });

        modelBuilder.Entity<CollectionItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("collection_items_pkey");

            entity.ToTable("collection_items");

            entity.HasIndex(e => new { e.CollectionId, e.ContestId }, "uq_collection_contest")
                .IsUnique()
                .HasFilter("(contest_id IS NOT NULL)");

            entity.HasIndex(e => new { e.CollectionId, e.ProblemId }, "uq_collection_problem")
                .IsUnique()
                .HasFilter("(problem_id IS NOT NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CollectionId).HasColumnName("collection_id");
            entity.Property(e => e.ContestId).HasColumnName("contest_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");

            entity.HasOne(d => d.Collection).WithMany(p => p.CollectionItems)
                .HasForeignKey(d => d.CollectionId)
                .HasConstraintName("fk_ci_collection");

            entity.HasOne(d => d.Contest).WithMany(p => p.CollectionItems)
                .HasForeignKey(d => d.ContestId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_ci_contest");

            entity.HasOne(d => d.Problem).WithMany(p => p.CollectionItems)
                .HasForeignKey(d => d.ProblemId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_ci_problem");
        });

        modelBuilder.Entity<CommentVote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("comment_votes_pkey");

            entity.ToTable("comment_votes");

            entity.HasIndex(e => e.CommentId, "idx_comment_votes_comment");

            entity.HasIndex(e => e.UserId, "idx_comment_votes_user");

            entity.HasIndex(e => new { e.UserId, e.CommentId }, "uq_user_comment_vote").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CommentId).HasColumnName("comment_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Vote).HasColumnName("vote");

            entity.HasOne(d => d.Comment).WithMany(p => p.CommentVotes)
                .HasForeignKey(d => d.CommentId)
                .HasConstraintName("fk_comment_votes_comment");

            entity.HasOne(d => d.User).WithMany(p => p.CommentVotes)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fk_comment_votes_user");
        });

        modelBuilder.Entity<ContentReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("content_reports_pkey");

            entity.ToTable("content_reports");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.ReporterId).HasColumnName("reporter_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValueSql("'pending'::character varying")
                .HasColumnName("status");
            entity.Property(e => e.TargetId).HasColumnName("target_id");
            entity.Property(e => e.TargetType)
                .HasMaxLength(50)
                .HasColumnName("target_type");

            entity.HasOne(d => d.Reporter).WithMany(p => p.ContentReports)
                .HasForeignKey(d => d.ReporterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("content_reports_reporter_id_fkey");
        });

        modelBuilder.Entity<Contest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("contests_pkey");

            entity.ToTable("contests");

            entity.HasIndex(e => e.Slug, "contests_slug_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AllowTeams)
                .HasDefaultValue(false)
                .HasColumnName("allow_teams");
            entity.Property(e => e.ConfigJson)
                .HasColumnType("jsonb")
                .HasColumnName("config_json");
            entity.Property(e => e.ContestType).HasColumnName("contest_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DescriptionMd).HasColumnName("description_md");
            entity.Property(e => e.EndAt).HasColumnName("end_at");
            entity.Property(e => e.FreezeAt).HasColumnName("freeze_at");
            entity.Property(e => e.InviteCode).HasColumnName("invite_code");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsVirtual)
                .HasDefaultValue(false)
                .HasColumnName("is_virtual");
            entity.Property(e => e.RemixOfContestId).HasColumnName("remix_of_contest_id");
            entity.Property(e => e.Rules).HasColumnName("rules");
            entity.Property(e => e.Slug).HasColumnName("slug");
            entity.Property(e => e.StartAt).HasColumnName("start_at");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UnfreezeAt).HasColumnName("unfreeze_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.VisibilityCode).HasColumnName("visibility_code");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ContestCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("contests_created_by_fkey");

            entity.HasOne(d => d.RemixOfContest).WithMany(p => p.InverseRemixOfContest)
                .HasForeignKey(d => d.RemixOfContestId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("contests_remix_of_contest_id_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ContestUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("contests_updated_by_fkey");
        });

        modelBuilder.Entity<ContestAnalytic>(entity =>
        {
            entity.HasKey(e => new { e.Day, e.ContestId }).HasName("contest_analytics_pkey");

            entity.ToTable("contest_analytics");

            entity.Property(e => e.Day).HasColumnName("day");
            entity.Property(e => e.ContestId).HasColumnName("contest_id");
            entity.Property(e => e.AcceptedCount)
                .HasDefaultValue(0)
                .HasColumnName("accepted_count");
            entity.Property(e => e.SubmissionsCount)
                .HasDefaultValue(0)
                .HasColumnName("submissions_count");
            entity.Property(e => e.UniqueTeams)
                .HasDefaultValue(0)
                .HasColumnName("unique_teams");
            entity.Property(e => e.UniqueUsers)
                .HasDefaultValue(0)
                .HasColumnName("unique_users");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Contest).WithMany(p => p.ContestAnalytics)
                .HasForeignKey(d => d.ContestId)
                .HasConstraintName("contest_analytics_contest_id_fkey");
        });

        modelBuilder.Entity<ContestHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("contest_history_pkey");

            entity.ToTable("contest_history");

            entity.Property(e => e.HistoryId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("history_id");
            entity.Property(e => e.ContestId).HasColumnName("contest_id");
            entity.Property(e => e.ParticipatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("participated_at");
            entity.Property(e => e.Ranking).HasColumnName("ranking");
            entity.Property(e => e.Score).HasColumnName("score");

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

            entity.ToTable("contest_problems");

            entity.HasIndex(e => new { e.ContestId, e.ProblemId }, "contest_problems_contest_id_problem_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Alias).HasColumnName("alias");
            entity.Property(e => e.ContestId).HasColumnName("contest_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DisplayIndex).HasColumnName("display_index");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MaxScore).HasColumnName("max_score");
            entity.Property(e => e.MemoryLimitKb).HasColumnName("memory_limit_kb");
            entity.Property(e => e.Ordinal).HasColumnName("ordinal");
            entity.Property(e => e.OutputLimitKb).HasColumnName("output_limit_kb");
            entity.Property(e => e.OverrideTestsetId).HasColumnName("override_testset_id");
            entity.Property(e => e.PenaltyPerWrong).HasColumnName("penalty_per_wrong");
            entity.Property(e => e.Points).HasColumnName("points");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.ScoringCode).HasColumnName("scoring_code");
            entity.Property(e => e.TimeLimitMs).HasColumnName("time_limit_ms");

            entity.HasOne(d => d.Contest).WithMany(p => p.ContestProblems)
                .HasForeignKey(d => d.ContestId)
                .HasConstraintName("contest_problems_contest_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ContestProblems)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("contest_problems_created_by_fkey");

            entity.HasOne(d => d.OverrideTestset).WithMany(p => p.ContestProblems)
                .HasForeignKey(d => d.OverrideTestsetId)
                .HasConstraintName("contest_problems_override_testset_id_fkey");

            entity.HasOne(d => d.Problem).WithMany(p => p.ContestProblems)
                .HasForeignKey(d => d.ProblemId)
                .HasConstraintName("contest_problems_problem_id_fkey");
        });

        modelBuilder.Entity<ContestScoreboard>(entity =>
        {
            entity.HasKey(e => new { e.ContestId, e.EntryId, e.ProblemId }).HasName("contest_scoreboard_pkey");

            entity.ToTable("contest_scoreboard");

            entity.Property(e => e.ContestId).HasColumnName("contest_id");
            entity.Property(e => e.EntryId).HasColumnName("entry_id");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.AcmAttempts)
                .HasDefaultValue(0)
                .HasColumnName("acm_attempts");
            entity.Property(e => e.AcmPenaltyTime)
                .HasDefaultValue(0)
                .HasColumnName("acm_penalty_time");
            entity.Property(e => e.AcmSolved)
                .HasDefaultValue(false)
                .HasColumnName("acm_solved");
            entity.Property(e => e.BestScore)
                .HasPrecision(18, 2)
                .HasColumnName("best_score");
            entity.Property(e => e.BestSubmissionId).HasColumnName("best_submission_id");
            entity.Property(e => e.FirstAcAt).HasColumnName("first_ac_at");
            entity.Property(e => e.LastScore)
                .HasPrecision(18, 2)
                .HasColumnName("last_score");
            entity.Property(e => e.LastSubmissionId).HasColumnName("last_submission_id");
            entity.Property(e => e.LastSubmitAt).HasColumnName("last_submit_at");
            entity.Property(e => e.VisibleUntil).HasColumnName("visible_until");

            entity.HasOne(d => d.BestSubmission).WithMany(p => p.ContestScoreboardBestSubmissions)
                .HasForeignKey(d => d.BestSubmissionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("contest_scoreboard_best_submission_id_fkey");

            entity.HasOne(d => d.Contest).WithMany(p => p.ContestScoreboards)
                .HasForeignKey(d => d.ContestId)
                .HasConstraintName("contest_scoreboard_contest_id_fkey");

            entity.HasOne(d => d.Entry).WithMany(p => p.ContestScoreboards)
                .HasForeignKey(d => d.EntryId)
                .HasConstraintName("contest_scoreboard_entry_id_fkey");

            entity.HasOne(d => d.LastSubmission).WithMany(p => p.ContestScoreboardLastSubmissions)
                .HasForeignKey(d => d.LastSubmissionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("contest_scoreboard_last_submission_id_fkey");

            entity.HasOne(d => d.Problem).WithMany(p => p.ContestScoreboards)
                .HasForeignKey(d => d.ProblemId)
                .HasConstraintName("contest_scoreboard_problem_id_fkey");
        });

        modelBuilder.Entity<ContestScoreboardEntry>(entity =>
        {
            entity.HasKey(e => new { e.ContestId, e.EntryId }).HasName("contest_scoreboard_entry_pkey");

            entity.ToTable("contest_scoreboard_entry");

            entity.Property(e => e.ContestId).HasColumnName("contest_id");
            entity.Property(e => e.EntryId).HasColumnName("entry_id");
            entity.Property(e => e.LastSolveAt).HasColumnName("last_solve_at");
            entity.Property(e => e.PenaltyTime)
                .HasDefaultValue(0)
                .HasColumnName("penalty_time");
            entity.Property(e => e.Rank).HasColumnName("rank");
            entity.Property(e => e.SolvedCount)
                .HasDefaultValue(0)
                .HasColumnName("solved_count");
            entity.Property(e => e.TotalScore)
                .HasPrecision(18, 2)
                .HasColumnName("total_score");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Contest).WithMany(p => p.ContestScoreboardEntries)
                .HasForeignKey(d => d.ContestId)
                .HasConstraintName("contest_scoreboard_entry_contest_id_fkey");

            entity.HasOne(d => d.Entry).WithMany(p => p.ContestScoreboardEntries)
                .HasForeignKey(d => d.EntryId)
                .HasConstraintName("contest_scoreboard_entry_entry_id_fkey");
        });

        modelBuilder.Entity<ContestTeam>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("contest_teams_pkey");

            entity.ToTable("contest_teams");

            entity.HasIndex(e => new { e.ContestId, e.TeamId }, "contest_teams_contest_id_team_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ContestId).HasColumnName("contest_id");
            entity.Property(e => e.JoinAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("join_at");
            entity.Property(e => e.Penalty)
                .HasDefaultValue(0)
                .HasColumnName("penalty");
            entity.Property(e => e.Rank).HasColumnName("rank");
            entity.Property(e => e.Score)
                .HasPrecision(18, 2)
                .HasColumnName("score");
            entity.Property(e => e.SolvedProblem)
                .HasDefaultValue(0)
                .HasColumnName("solved_problem");
            entity.Property(e => e.SubmissionsCount)
                .HasDefaultValue(0)
                .HasColumnName("submissions_count");
            entity.Property(e => e.TeamId).HasColumnName("team_id");

            entity.HasOne(d => d.Contest).WithMany(p => p.ContestTeams)
                .HasForeignKey(d => d.ContestId)
                .HasConstraintName("contest_teams_contest_id_fkey");

            entity.HasOne(d => d.Team).WithMany(p => p.ContestTeams)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("contest_teams_team_id_fkey");
        });

        modelBuilder.Entity<DiscussionComment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("discussion_comments_pkey");

            entity.ToTable("discussion_comments");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.DiscussionId).HasColumnName("discussion_id");
            entity.Property(e => e.IsHidden)
                .HasDefaultValue(false)
                .HasColumnName("is_hidden");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VoteCount)
                .HasDefaultValue(0)
                .HasColumnName("vote_count");

            entity.HasOne(d => d.Discussion).WithMany(p => p.DiscussionComments)
                .HasForeignKey(d => d.DiscussionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("discussion_comments_discussion_id_fkey");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("discussion_comments_parent_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.DiscussionComments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("discussion_comments_user_id_fkey");
        });

        modelBuilder.Entity<Editorial>(entity =>
        {
            entity.HasKey(e => e.EditorialId).HasName("editorials_pkey");

            entity.ToTable("editorials");

            entity.Property(e => e.EditorialId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("editorial_id");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.StorageId).HasColumnName("storage_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasOne(d => d.Author).WithMany(p => p.Editorials)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("editorials_author_id_fkey");

            entity.HasOne(d => d.Storage).WithMany(p => p.Editorials)
                .HasForeignKey(d => d.StorageId)
                .HasConstraintName("editorials_storage_id_fkey");
        });

        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.HasKey(e => e.VerificationId).HasName("email_verification_pkey");

            entity.ToTable("email_verification");

            entity.HasIndex(e => e.UserId, "unique_verification_user").IsUnique();

            entity.Property(e => e.VerificationId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("verification_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Token).HasColumnName("token");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.EmailVerification)
                .HasForeignKey<EmailVerification>(d => d.UserId)
                .HasConstraintName("email_verification_user_id_fkey");
        });

        modelBuilder.Entity<JudgeJob>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("judge_jobs_pkey");

            entity.ToTable("judge_jobs");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Attempts)
                .HasDefaultValue(0)
                .HasColumnName("attempts");
            entity.Property(e => e.DequeuedAt).HasColumnName("dequeued_at");
            entity.Property(e => e.DequeuedByWorkerId).HasColumnName("dequeued_by_worker_id");
            entity.Property(e => e.EnqueueAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("enqueue_at");
            entity.Property(e => e.LastError).HasColumnName("last_error");
            entity.Property(e => e.Priority)
                .HasDefaultValue(0)
                .HasColumnName("priority");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.SubmissionId).HasColumnName("submission_id");
            entity.Property(e => e.TriggerReason).HasColumnName("trigger_reason");
            entity.Property(e => e.TriggerType)
                .HasDefaultValueSql("'submit'::text")
                .HasColumnName("trigger_type");
            entity.Property(e => e.TriggeredByUserId).HasColumnName("triggered_by_user_id");

            entity.HasOne(d => d.DequeuedByWorker).WithMany(p => p.JudgeJobs)
                .HasForeignKey(d => d.DequeuedByWorkerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("judge_jobs_dequeued_by_worker_id_fkey");

            entity.HasOne(d => d.Submission).WithMany(p => p.JudgeJobs)
                .HasForeignKey(d => d.SubmissionId)
                .HasConstraintName("judge_jobs_submission_id_fkey");

            entity.HasOne(d => d.TriggeredByUser).WithMany(p => p.JudgeJobs)
                .HasForeignKey(d => d.TriggeredByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("judge_jobs_triggered_by_user_id_fkey");
        });

        modelBuilder.Entity<JudgeRun>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("judge_runs_pkey");

            entity.ToTable("judge_runs");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompileExitCode).HasColumnName("compile_exit_code");
            entity.Property(e => e.CompileLogBlobId).HasColumnName("compile_log_blob_id");
            entity.Property(e => e.CompileTimeMs).HasColumnName("compile_time_ms");
            entity.Property(e => e.DockerImage).HasColumnName("docker_image");
            entity.Property(e => e.FinishedAt).HasColumnName("finished_at");
            entity.Property(e => e.Limits)
                .HasColumnType("jsonb")
                .HasColumnName("limits");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.RuntimeId).HasColumnName("runtime_id");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("started_at");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.SubmissionId).HasColumnName("submission_id");
            entity.Property(e => e.TotalMemoryKb).HasColumnName("total_memory_kb");
            entity.Property(e => e.TotalTimeMs).HasColumnName("total_time_ms");
            entity.Property(e => e.WorkerId).HasColumnName("worker_id");

            entity.HasOne(d => d.CompileLogBlob).WithMany(p => p.JudgeRuns)
                .HasForeignKey(d => d.CompileLogBlobId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("judge_runs_compile_log_blob_id_fkey");

            entity.HasOne(d => d.Runtime).WithMany(p => p.JudgeRuns)
                .HasForeignKey(d => d.RuntimeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("judge_runs_runtime_id_fkey");

            entity.HasOne(d => d.Submission).WithMany(p => p.JudgeRuns)
                .HasForeignKey(d => d.SubmissionId)
                .HasConstraintName("judge_runs_submission_id_fkey");

            entity.HasOne(d => d.Worker).WithMany(p => p.JudgeRuns)
                .HasForeignKey(d => d.WorkerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("judge_runs_worker_id_fkey");
        });

        modelBuilder.Entity<JudgeWorker>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("judge_workers_pkey");

            entity.ToTable("judge_workers");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Capabilities)
                .HasColumnType("jsonb")
                .HasColumnName("capabilities");
            entity.Property(e => e.LastSeenAt).HasColumnName("last_seen_at");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Version).HasColumnName("version");
        });

        modelBuilder.Entity<ModerationAction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("moderation_actions_pkey");

            entity.ToTable("moderation_actions");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActionType)
                .HasMaxLength(50)
                .HasColumnName("action_type");
            entity.Property(e => e.AdminId).HasColumnName("admin_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.ReportId).HasColumnName("report_id");

            entity.HasOne(d => d.Admin).WithMany(p => p.ModerationActions)
                .HasForeignKey(d => d.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("moderation_actions_admin_id_fkey");

            entity.HasOne(d => d.Report).WithMany(p => p.ModerationActions)
                .HasForeignKey(d => d.ReportId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("moderation_actions_report_id_fkey");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("notifications_pkey");

            entity.ToTable("notifications");

            entity.Property(e => e.NotificationId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("notification_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.ScopeId).HasColumnName("scope_id");
            entity.Property(e => e.ScopeType).HasColumnName("scope_type");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.NotificationCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("notifications_created_by_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.NotificationUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("notifications_user_id_fkey");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("payment_pkey");

            entity.ToTable("payment");

            entity.Property(e => e.PaymentId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("payment_id");
            entity.Property(e => e.AmountMoney)
                .HasPrecision(18, 2)
                .HasColumnName("amount_money");
            entity.Property(e => e.BankCode).HasColumnName("bank_code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasDefaultValueSql("'vnd'::text")
                .HasColumnName("currency");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method");
            entity.Property(e => e.PaymentTxn).HasColumnName("payment_txn");
            entity.Property(e => e.ProviderName).HasColumnName("provider_name");
            entity.Property(e => e.ProviderTxId).HasColumnName("provider_tx_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'pending'::text")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("payment_user_id_fkey");
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.PermissionId).HasName("permission_pkey");

            entity.ToTable("permission");

            entity.HasIndex(e => e.PermissionCode, "permission_permission_code_key").IsUnique();

            entity.Property(e => e.PermissionId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("permission_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.PermissionCode).HasColumnName("permission_code");
            entity.Property(e => e.PermissionDesc).HasColumnName("permission_desc");
        });

        modelBuilder.Entity<Problem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("problems_pkey");

            entity.ToTable("problems");

            entity.HasIndex(e => e.Slug, "problems_slug_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AcceptancePercent)
                .HasPrecision(5, 2)
                .HasColumnName("acceptance_percent");
            entity.Property(e => e.ApprovedAt).HasColumnName("approved_at");
            entity.Property(e => e.ApprovedByUserId).HasColumnName("approved_by_user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DescriptionMd).HasColumnName("description_md");
            entity.Property(e => e.Difficulty).HasColumnName("difficulty");
            entity.Property(e => e.DisplayIndex).HasColumnName("display_index");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MemoryLimitKb).HasColumnName("memory_limit_kb");
            entity.Property(e => e.PublishedAt).HasColumnName("published_at");
            entity.Property(e => e.ScoringCode).HasColumnName("scoring_code");
            entity.Property(e => e.Slug).HasColumnName("slug");
            entity.Property(e => e.StatusCode)
                .HasDefaultValueSql("'draft'::text")
                .HasColumnName("status_code");
            entity.Property(e => e.TimeLimitMs).HasColumnName("time_limit_ms");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.TypeCode).HasColumnName("type_code");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.VisibilityCode).HasColumnName("visibility_code");

            entity.HasOne(d => d.ApprovedByUser).WithMany(p => p.ProblemApprovedByUsers)
                .HasForeignKey(d => d.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("problems_approved_by_user_id_fkey");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ProblemCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("problems_created_by_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.ProblemUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
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

            entity.ToTable("problem_discussions");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.IsLocked)
                .HasDefaultValue(false)
                .HasColumnName("is_locked");
            entity.Property(e => e.IsPinned)
                .HasDefaultValue(false)
                .HasColumnName("is_pinned");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Problem).WithMany(p => p.ProblemDiscussions)
                .HasForeignKey(d => d.ProblemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("problem_discussions_problem_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.ProblemDiscussions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("problem_discussions_user_id_fkey");
        });

        modelBuilder.Entity<ProblemEditorial>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("problem_editorials_pkey");

            entity.ToTable("problem_editorials");

            entity.HasIndex(e => e.ProblemId, "problem_editorials_problem_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AuthorId).HasColumnName("author_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Author).WithMany(p => p.ProblemEditorials)
                .HasForeignKey(d => d.AuthorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("problem_editorials_author_id_fkey");

            entity.HasOne(d => d.Problem).WithOne(p => p.ProblemEditorial)
                .HasForeignKey<ProblemEditorial>(d => d.ProblemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("problem_editorials_problem_id_fkey");
        });

        modelBuilder.Entity<ProblemStat>(entity =>
        {
            entity.HasKey(e => e.ProblemId).HasName("problem_stats_pkey");

            entity.ToTable("problem_stats");

            entity.Property(e => e.ProblemId)
                .ValueGeneratedNever()
                .HasColumnName("problem_id");
            entity.Property(e => e.AcceptedCount)
                .HasDefaultValue(0L)
                .HasColumnName("accepted_count");
            entity.Property(e => e.AvgMemoryKb).HasColumnName("avg_memory_kb");
            entity.Property(e => e.AvgTimeMs).HasColumnName("avg_time_ms");
            entity.Property(e => e.SubmissionsCount)
                .HasDefaultValue(0L)
                .HasColumnName("submissions_count");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Problem).WithOne(p => p.ProblemStat)
                .HasForeignKey<ProblemStat>(d => d.ProblemId)
                .HasConstraintName("problem_stats_problem_id_fkey");
        });

        modelBuilder.Entity<Provider>(entity =>
        {
            entity.HasKey(e => e.ProviderId).HasName("provider_pkey");

            entity.ToTable("provider");

            entity.HasIndex(e => e.ProviderCode, "provider_provider_code_key").IsUnique();

            entity.Property(e => e.ProviderId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("provider_id");
            entity.Property(e => e.Enabled)
                .HasDefaultValue(true)
                .HasColumnName("enabled");
            entity.Property(e => e.Issuer).HasColumnName("issuer");
            entity.Property(e => e.ProviderCode).HasColumnName("provider_code");
            entity.Property(e => e.ProviderDisplayName).HasColumnName("provider_display_name");
            entity.Property(e => e.ProviderIcon).HasColumnName("provider_icon");
        });

        modelBuilder.Entity<RatingHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("rating_history_pkey");

            entity.ToTable("rating_history");

            entity.HasIndex(e => new { e.UserId, e.Id }, "unique_user_contest_rating_history").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ContestId).HasColumnName("contest_id");
            entity.Property(e => e.NewRating).HasColumnName("new_rating");
            entity.Property(e => e.OldRating).HasColumnName("old_rating");
            entity.Property(e => e.ProcessedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("processed_at");
            entity.Property(e => e.RankInContest).HasColumnName("rank_in_contest");
            entity.Property(e => e.RatingChange).HasColumnName("rating_change");
            entity.Property(e => e.ScopeId).HasColumnName("scope_id");
            entity.Property(e => e.ScopeType)
                .HasDefaultValueSql("'global'::text")
                .HasColumnName("scope_type");
            entity.Property(e => e.Score)
                .HasPrecision(18, 2)
                .HasColumnName("score");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Contest).WithMany(p => p.RatingHistories)
                .HasForeignKey(d => d.ContestId)
                .HasConstraintName("rating_history_contest_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.RatingHistories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("rating_history_user_id_fkey");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.TokenId).HasName("refresh_token_pkey");

            entity.ToTable("refresh_token");

            entity.HasIndex(e => e.TokenHash, "refresh_token_token_hash_key").IsUnique();

            entity.Property(e => e.TokenId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("token_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpireAt).HasColumnName("expire_at");
            entity.Property(e => e.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.SessionId).HasColumnName("session_id");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash");

            entity.HasOne(d => d.ReplacedByToken).WithMany(p => p.InverseReplacedByToken)
                .HasForeignKey(d => d.ReplacedByTokenId)
                .HasConstraintName("refresh_token_replaced_by_token_id_fkey");

            entity.HasOne(d => d.Session).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("refresh_token_session_id_fkey");
        });

        modelBuilder.Entity<ReportsExportHistory>(entity =>
        {
            entity.HasKey(e => e.ExportId).HasName("reports_export_history_pkey");

            entity.ToTable("reports_export_history");

            entity.Property(e => e.ExportId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("export_id");
            entity.Property(e => e.ExtensionType).HasColumnName("extension_type");
            entity.Property(e => e.FilePath).HasColumnName("file_path");
            entity.Property(e => e.GeneratedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("generated_at");
            entity.Property(e => e.GeneratedBy).HasColumnName("generated_by");
            entity.Property(e => e.ReportType).HasColumnName("report_type");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'pending'::text")
                .HasColumnName("status");

            entity.HasOne(d => d.GeneratedByNavigation).WithMany(p => p.ReportsExportHistories)
                .HasForeignKey(d => d.GeneratedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("reports_export_history_generated_by_fkey");
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("result_pkey");

            entity.ToTable("result");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActualOutput).HasColumnName("actual_output");
            entity.Property(e => e.CheckerMessage).HasColumnName("checker_message");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExitCode).HasColumnName("exit_code");
            entity.Property(e => e.ExpectedOutput).HasColumnName("expected_output");
            entity.Property(e => e.Input).HasColumnName("input");
            entity.Property(e => e.JudgeRunId).HasColumnName("judge_run_id");
            entity.Property(e => e.MemoryKb).HasColumnName("memory_kb");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.OutputUrl).HasColumnName("output_url");
            entity.Property(e => e.RuntimeMs).HasColumnName("runtime_ms");
            entity.Property(e => e.Signal).HasColumnName("signal");
            entity.Property(e => e.StatusCode).HasColumnName("status_code");
            entity.Property(e => e.StderrBlobId).HasColumnName("stderr_blob_id");
            entity.Property(e => e.StdoutBlobId).HasColumnName("stdout_blob_id");
            entity.Property(e => e.SubmissionId).HasColumnName("submission_id");
            entity.Property(e => e.TestcaseId).HasColumnName("testcase_id");
            entity.Property(e => e.Type).HasColumnName("type");

            entity.HasOne(d => d.JudgeRun).WithMany(p => p.Results)
                .HasForeignKey(d => d.JudgeRunId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("result_judge_run_id_fkey");

            entity.HasOne(d => d.StderrBlob).WithMany(p => p.ResultStderrBlobs)
                .HasForeignKey(d => d.StderrBlobId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("result_stderr_blob_id_fkey");

            entity.HasOne(d => d.StdoutBlob).WithMany(p => p.ResultStdoutBlobs)
                .HasForeignKey(d => d.StdoutBlobId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("result_stdout_blob_id_fkey");

            entity.HasOne(d => d.Submission).WithMany(p => p.Results)
                .HasForeignKey(d => d.SubmissionId)
                .HasConstraintName("result_submission_id_fkey");

            entity.HasOne(d => d.Testcase).WithMany(p => p.Results)
                .HasForeignKey(d => d.TestcaseId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("result_testcase_id_fkey");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("role_pkey");

            entity.ToTable("role");

            entity.HasIndex(e => e.RoleCode, "role_role_code_key").IsUnique();

            entity.Property(e => e.RoleId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("role_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.IsSystem)
                .HasDefaultValue(false)
                .HasColumnName("is_system");
            entity.Property(e => e.RoleCode).HasColumnName("role_code");
            entity.Property(e => e.RoleDesc).HasColumnName("role_desc");
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId }).HasName("role_permission_pkey");

            entity.ToTable("role_permission");

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.Status)
                .HasDefaultValue(true)
                .HasColumnName("status");

            entity.HasOne(d => d.Permission).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.PermissionId)
                .HasConstraintName("role_permission_permission_id_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("role_permission_role_id_fkey");
        });

        modelBuilder.Entity<RunMetric>(entity =>
        {
            entity.HasKey(e => e.MetricId).HasName("run_metrics_pkey");

            entity.ToTable("run_metrics");

            entity.Property(e => e.MetricId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("metric_id");
            entity.Property(e => e.CpuUsage)
                .HasPrecision(5, 2)
                .HasColumnName("cpu_usage");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.MemoryKb).HasColumnName("memory_kb");
            entity.Property(e => e.PassedTestcases).HasColumnName("passed_testcases");
            entity.Property(e => e.RuntimeMs).HasColumnName("runtime_ms");
            entity.Property(e => e.SubmissionId).HasColumnName("submission_id");
            entity.Property(e => e.TotalTestcases).HasColumnName("total_testcases");
        });

        modelBuilder.Entity<Runtime>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("runtime_pkey");

            entity.ToTable("runtime");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.DefaultMemoryLimitKb)
                .HasDefaultValue(262144)
                .HasColumnName("default_memory_limit_kb");
            entity.Property(e => e.DefaultTimeLimitMs)
                .HasDefaultValue(1000)
                .HasColumnName("default_time_limit_ms");
            entity.Property(e => e.ImageRef).HasColumnName("image_ref");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.RuntimeName).HasColumnName("runtime_name");
            entity.Property(e => e.RuntimeVersion).HasColumnName("runtime_version");
        });

        modelBuilder.Entity<ScoreRecalcJob>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("score_recalc_jobs_pkey");

            entity.ToTable("score_recalc_jobs");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ContestEntryId).HasColumnName("contest_entry_id");
            entity.Property(e => e.ContestId).HasColumnName("contest_id");
            entity.Property(e => e.ContestProblemId).HasColumnName("contest_problem_id");
            entity.Property(e => e.EnqueueAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("enqueue_at");
            entity.Property(e => e.Errors)
                .HasColumnType("jsonb")
                .HasColumnName("errors");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.Property(e => e.Scope).HasColumnName("scope");
            entity.Property(e => e.Status).HasColumnName("status");

            entity.HasOne(d => d.ContestEntry).WithMany(p => p.ScoreRecalcJobs)
                .HasForeignKey(d => d.ContestEntryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("score_recalc_jobs_contest_entry_id_fkey");

            entity.HasOne(d => d.Contest).WithMany(p => p.ScoreRecalcJobs)
                .HasForeignKey(d => d.ContestId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("score_recalc_jobs_contest_id_fkey");

            entity.HasOne(d => d.ContestProblem).WithMany(p => p.ScoreRecalcJobs)
                .HasForeignKey(d => d.ContestProblemId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("score_recalc_jobs_contest_problem_id_fkey");
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.SemesterId).HasName("semester_pkey");

            entity.ToTable("semester");

            entity.HasIndex(e => e.Code, "semester_code_key").IsUnique();

            entity.Property(e => e.SemesterId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("semester_id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.EndAt).HasColumnName("end_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.StartAt).HasColumnName("start_at");
        });

        modelBuilder.Entity<Solution>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("solution_pkey");

            entity.ToTable("solution");

            entity.HasIndex(e => new { e.UserId, e.ProblemId }, "solution_user_id_problem_id_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.DescMd).HasColumnName("desc_md");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.RuntimeId).HasColumnName("runtime_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Problem).WithMany(p => p.Solutions)
                .HasForeignKey(d => d.ProblemId)
                .HasConstraintName("solution_problem_id_fkey");

            entity.HasOne(d => d.Runtime).WithMany(p => p.Solutions)
                .HasForeignKey(d => d.RuntimeId)
                .HasConstraintName("solution_runtime_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Solutions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("solution_user_id_fkey");
        });

        modelBuilder.Entity<StorageFile>(entity =>
        {
            entity.HasKey(e => e.StorageId).HasName("storage_files_pkey");

            entity.ToTable("storage_files");

            entity.Property(e => e.StorageId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("storage_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.FilePath).HasColumnName("file_path");
            entity.Property(e => e.FileSize).HasColumnName("file_size");
            entity.Property(e => e.FileType).HasColumnName("file_type");
            entity.Property(e => e.HashChecksum).HasColumnName("hash_checksum");
            entity.Property(e => e.IsPrivate)
                .HasDefaultValue(true)
                .HasColumnName("is_private");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");

            entity.HasOne(d => d.Owner).WithMany(p => p.StorageFiles)
                .HasForeignKey(d => d.OwnerId)
                .HasConstraintName("storage_files_owner_id_fkey");
        });

        modelBuilder.Entity<StudyPlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("study_plans_pkey");

            entity.ToTable("study_plans");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatorId).HasColumnName("creator_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsPublic)
                .HasDefaultValue(true)
                .HasColumnName("is_public");
            entity.Property(e => e.Title).HasColumnName("title");

            entity.HasOne(d => d.Creator).WithMany(p => p.StudyPlans)
                .HasForeignKey(d => d.CreatorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("study_plans_creator_id_fkey");
        });

        modelBuilder.Entity<StudyPlanItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("study_plan_items_pkey");

            entity.ToTable("study_plan_items");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.OrderIndex).HasColumnName("order_index");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.StudyPlanId).HasColumnName("study_plan_id");

            entity.HasOne(d => d.Problem).WithMany(p => p.StudyPlanItems)
                .HasForeignKey(d => d.ProblemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("study_plan_items_problem_id_fkey");

            entity.HasOne(d => d.StudyPlan).WithMany(p => p.StudyPlanItems)
                .HasForeignKey(d => d.StudyPlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("study_plan_items_study_plan_id_fkey");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.SubjectId).HasName("subject_pkey");

            entity.ToTable("subject");

            entity.HasIndex(e => e.Code, "subject_code_key").IsUnique();

            entity.Property(e => e.SubjectId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("subject_id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("submissions_pkey");

            entity.ToTable("submissions");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CodeArtifactId).HasColumnName("code_artifact_id");
            entity.Property(e => e.CodeHash).HasColumnName("code_hash");
            entity.Property(e => e.CodeSize)
                .HasDefaultValue(0)
                .HasColumnName("code_size");
            entity.Property(e => e.ContestProblemId).HasColumnName("contest_problem_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomInput).HasColumnName("custom_input");
            entity.Property(e => e.FinalScore)
                .HasPrecision(18, 2)
                .HasColumnName("final_score");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.JudgedAt).HasColumnName("judged_at");
            entity.Property(e => e.MemoryKb).HasColumnName("memory_kb");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.RuntimeId).HasColumnName("runtime_id");
            entity.Property(e => e.StatusCode)
                .HasDefaultValueSql("'queued'::text")
                .HasColumnName("status_code");
            entity.Property(e => e.StorageBlobId).HasColumnName("storage_blob_id");
            entity.Property(e => e.SubmissionType)
                .HasDefaultValueSql("'practice'::text")
                .HasColumnName("submission_type");
            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.TestcaseId).HasColumnName("testcase_id");
            entity.Property(e => e.TestsetId).HasColumnName("testset_id");
            entity.Property(e => e.TimeMs).HasColumnName("time_ms");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VerdictCode).HasColumnName("verdict_code");

            entity.HasOne(d => d.CodeArtifact).WithMany(p => p.SubmissionCodeArtifacts)
                .HasForeignKey(d => d.CodeArtifactId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("submissions_code_artifact_id_fkey");

            entity.HasOne(d => d.ContestProblem).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.ContestProblemId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("submissions_contest_problem_id_fkey");

            entity.HasOne(d => d.Problem).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.ProblemId)
                .HasConstraintName("submissions_problem_id_fkey");

            entity.HasOne(d => d.Runtime).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.RuntimeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("submissions_runtime_id_fkey");

            entity.HasOne(d => d.StorageBlob).WithMany(p => p.SubmissionStorageBlobs)
                .HasForeignKey(d => d.StorageBlobId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("submissions_storage_blob_id_fkey");

            entity.HasOne(d => d.Team).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.TeamId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("submissions_team_id_fkey");

            entity.HasOne(d => d.Testcase).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.TestcaseId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("submissions_testcase_id_fkey");

            entity.HasOne(d => d.Testset).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.TestsetId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("submissions_testset_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("submissions_user_id_fkey");
        });

        modelBuilder.Entity<SubmissionQuotum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("submission_quota_pkey");

            entity.ToTable("submission_quota");

            entity.HasIndex(e => new { e.UserId, e.ProblemId, e.Date }, "submission_quota_user_id_problem_id_date_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Count)
                .HasDefaultValue(0)
                .HasColumnName("count");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.QuotaLimit).HasColumnName("quota_limit");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Problem).WithMany(p => p.SubmissionQuota)
                .HasForeignKey(d => d.ProblemId)
                .HasConstraintName("submission_quota_problem_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.SubmissionQuota)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("submission_quota_user_id_fkey");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tag_pkey");

            entity.ToTable("tag");

            entity.HasIndex(e => e.Name, "ux_tags_name").IsUnique();

            entity.HasIndex(e => e.Slug, "ux_tags_slug").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.Color).HasColumnName("color");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Icon).HasColumnName("icon");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Slug).HasColumnName("slug");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.TagCreatedByNavigations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("tag_created_by_fkey");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.TagUpdatedByNavigations)
                .HasForeignKey(d => d.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("tag_updated_by_fkey");
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("team_pkey");

            entity.ToTable("team");

            entity.HasIndex(e => e.InviteCode, "team_invite_code_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.InviteCode).HasColumnName("invite_code");
            entity.Property(e => e.IsPersonal)
                .HasDefaultValue(false)
                .HasColumnName("is_personal");
            entity.Property(e => e.LeaderId).HasColumnName("leader_id");
            entity.Property(e => e.TeamName).HasColumnName("team_name");
            entity.Property(e => e.TeamSize)
                .HasDefaultValue(1)
                .HasColumnName("team_size");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Leader).WithMany(p => p.Teams)
                .HasForeignKey(d => d.LeaderId)
                .HasConstraintName("team_leader_id_fkey");
        });

        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(e => new { e.TeamId, e.UserId }).HasName("team_members_pkey");

            entity.ToTable("team_members");

            entity.Property(e => e.TeamId).HasColumnName("team_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("joined_at");

            entity.HasOne(d => d.Team).WithMany(p => p.TeamMembers)
                .HasForeignKey(d => d.TeamId)
                .HasConstraintName("team_members_team_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.TeamMembers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("team_members_user_id_fkey");
        });

        modelBuilder.Entity<Testcase>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("testcases_pkey");

            entity.ToTable("testcases");

            entity.HasIndex(e => new { e.TestsetId, e.Ordinal }, "testcases_testset_id_ordinal_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ExpectedOutput).HasColumnName("expected_output");
            entity.Property(e => e.ExpireAt).HasColumnName("expire_at");
            entity.Property(e => e.Input).HasColumnName("input");
            entity.Property(e => e.IsSample)
                .HasDefaultValue(false)
                .HasColumnName("is_sample");
            entity.Property(e => e.Ordinal).HasColumnName("ordinal");
            entity.Property(e => e.StorageBlobId).HasColumnName("storage_blob_id");
            entity.Property(e => e.TestsetId).HasColumnName("testset_id");
            entity.Property(e => e.Weight)
                .HasDefaultValue(1)
                .HasColumnName("weight");

            entity.HasOne(d => d.StorageBlob).WithMany(p => p.Testcases)
                .HasForeignKey(d => d.StorageBlobId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("testcases_storage_blob_id_fkey");

            entity.HasOne(d => d.Testset).WithMany(p => p.Testcases)
                .HasForeignKey(d => d.TestsetId)
                .HasConstraintName("testcases_testset_id_fkey");
        });

        modelBuilder.Entity<Testset>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("testsets_pkey");

            entity.ToTable("testsets");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.ExpireAt).HasColumnName("expire_at");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.StorageBlobId).HasColumnName("storage_blob_id");
            entity.Property(e => e.Type).HasColumnName("type");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Testsets)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("testsets_created_by_fkey");

            entity.HasOne(d => d.Problem).WithMany(p => p.Testsets)
                .HasForeignKey(d => d.ProblemId)
                .HasConstraintName("testsets_problem_id_fkey");

            entity.HasOne(d => d.StorageBlob).WithMany(p => p.Testsets)
                .HasForeignKey(d => d.StorageBlobId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("testsets_storage_blob_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.HasIndex(e => e.Username, "users_username_key").IsUnique();

            entity.Property(e => e.UserId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("user_id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.EmailVerified)
                .HasDefaultValue(false)
                .HasColumnName("email_verified");
            entity.Property(e => e.FirstName).HasColumnName("first_name");
            entity.Property(e => e.LanguagePreference)
                .HasDefaultValueSql("'en'::text")
                .HasColumnName("language_preference");
            entity.Property(e => e.LastName).HasColumnName("last_name");
            entity.Property(e => e.Password).HasColumnName("password");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Status)
                .HasDefaultValue(true)
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.Username).HasColumnName("username");
            entity.Property(e => e.RollNumber).HasColumnName("roll_number");
            entity.Property(e => e.MemberCode).HasColumnName("member_code");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("users_role_id_fkey");
        });

        modelBuilder.Entity<UserBadge>(entity =>
        {
            entity.HasKey(e => e.UserBadgesId).HasName("user_badges_pkey");

            entity.ToTable("user_badges");

            entity.HasIndex(e => new { e.UserId, e.BadgeId }, "unique_user_badge").IsUnique();

            entity.Property(e => e.UserBadgesId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("user_badges_id");
            entity.Property(e => e.AwardedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("awarded_at");
            entity.Property(e => e.BadgeId).HasColumnName("badge_id");
            entity.Property(e => e.ContextType).HasColumnName("context_type");
            entity.Property(e => e.MetaJson)
                .HasColumnType("jsonb")
                .HasColumnName("meta_json");
            entity.Property(e => e.SourceId).HasColumnName("source_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Badge).WithMany(p => p.UserBadges)
                .HasForeignKey(d => d.BadgeId)
                .HasConstraintName("user_badges_badge_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserBadges)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_badges_user_id_fkey");
        });

        modelBuilder.Entity<UserNotificationSetting>(entity =>
        {
            entity.HasKey(e => e.SettingId).HasName("user_notification_settings_pkey");

            entity.ToTable("user_notification_settings");

            entity.HasIndex(e => e.UserId, "user_notification_settings_user_id_key").IsUnique();

            entity.Property(e => e.SettingId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("setting_id");
            entity.Property(e => e.ReceiveEmail)
                .HasDefaultValue(true)
                .HasColumnName("receive_email");
            entity.Property(e => e.ReceivePush)
                .HasDefaultValue(true)
                .HasColumnName("receive_push");
            entity.Property(e => e.ReceiveSystem)
                .HasDefaultValue(true)
                .HasColumnName("receive_system");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.UserNotificationSetting)
                .HasForeignKey<UserNotificationSetting>(d => d.UserId)
                .HasConstraintName("user_notification_settings_user_id_fkey");
        });

        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.PermissionId }).HasName("user_permission_pkey");

            entity.ToTable("user_permission");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.PermissionId).HasColumnName("permission_id");
            entity.Property(e => e.EffectiveAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("effective_at");
            entity.Property(e => e.ExpireAt).HasColumnName("expire_at");
            entity.Property(e => e.Status)
                .HasDefaultValue(true)
                .HasColumnName("status");

            entity.HasOne(d => d.Permission).WithMany(p => p.UserPermissions)
                .HasForeignKey(d => d.PermissionId)
                .HasConstraintName("user_permission_permission_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserPermissions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_permission_user_id_fkey");
        });

        modelBuilder.Entity<UserProblemStat>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.ProblemId }).HasName("user_problem_stats_pkey");

            entity.ToTable("user_problem_stats");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.Attempts)
                .HasDefaultValue(0)
                .HasColumnName("attempts");
            entity.Property(e => e.BestSubmissionId).HasColumnName("best_submission_id");
            entity.Property(e => e.LastSubmissionAt).HasColumnName("last_submission_at");
            entity.Property(e => e.Solved)
                .HasDefaultValue(false)
                .HasColumnName("solved");

            entity.HasOne(d => d.BestSubmission).WithMany(p => p.UserProblemStats)
                .HasForeignKey(d => d.BestSubmissionId)
                .HasConstraintName("user_problem_stats_best_submission_id_fkey");

            entity.HasOne(d => d.Problem).WithMany(p => p.UserProblemStats)
                .HasForeignKey(d => d.ProblemId)
                .HasConstraintName("user_problem_stats_problem_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserProblemStats)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_problem_stats_user_id_fkey");
        });

        modelBuilder.Entity<UserProvider>(entity =>
        {
            entity.HasKey(e => e.UserProviderId).HasName("user_provider_pkey");

            entity.ToTable("user_provider");

            entity.HasIndex(e => new { e.UserId, e.ProviderId }, "unique_user_provider").IsUnique();

            entity.Property(e => e.UserProviderId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("user_provider_id");
            entity.Property(e => e.AccessTokenEnc).HasColumnName("access_token_enc");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpireAt).HasColumnName("expire_at");
            entity.Property(e => e.ProviderEmail).HasColumnName("provider_email");
            entity.Property(e => e.ProviderId).HasColumnName("provider_id");
            entity.Property(e => e.ProviderProfile)
                .HasColumnType("jsonb")
                .HasColumnName("provider_profile");
            entity.Property(e => e.ProviderSubject).HasColumnName("provider_subject");
            entity.Property(e => e.RefreshTokenEnc).HasColumnName("refresh_token_enc");
            entity.Property(e => e.Scope).HasColumnName("scope");
            entity.Property(e => e.TokenType).HasColumnName("token_type");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Provider).WithMany(p => p.UserProviders)
                .HasForeignKey(d => d.ProviderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_provider_provider_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserProviders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_provider_user_id_fkey");
        });

        modelBuilder.Entity<UserRating>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_ratings_pkey");

            entity.ToTable("user_ratings");

            entity.HasIndex(e => new { e.UserId, e.ScopeType, e.ScopeId }, "unique_user_rating_scope").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.LastCompetedAt).HasColumnName("last_competed_at");
            entity.Property(e => e.MaxRating)
                .HasDefaultValue(1000)
                .HasColumnName("max_rating");
            entity.Property(e => e.RankTitle)
                .HasDefaultValueSql("'newbie'::text")
                .HasColumnName("rank_title");
            entity.Property(e => e.Rating)
                .HasDefaultValue(1000)
                .HasColumnName("rating");
            entity.Property(e => e.ScopeId).HasColumnName("scope_id");
            entity.Property(e => e.ScopeType)
                .HasDefaultValueSql("'global'::text")
                .HasColumnName("scope_type");
            entity.Property(e => e.TimesPlayed)
                .HasDefaultValue(0)
                .HasColumnName("times_played");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Volatility)
                .HasDefaultValue(0)
                .HasColumnName("volatility");

            entity.HasOne(d => d.User).WithMany(p => p.UserRatings)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_ratings_user_id_fkey");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId }).HasName("user_role_pkey");

            entity.ToTable("user_role");

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("assigned_at");
            entity.Property(e => e.AssignedBy).HasColumnName("assigned_by");

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.UserRoleAssignedByNavigations)
                .HasForeignKey(d => d.AssignedBy)
                .HasConstraintName("user_role_assigned_by_fkey");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("user_role_role_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoleUsers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_role_user_id_fkey");
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("user_session_pkey");

            entity.ToTable("user_session");

            entity.Property(e => e.SessionId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("session_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address");
            entity.Property(e => e.LastSeenAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("last_seen_at");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserSessions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_session_user_id_fkey");
        });

        modelBuilder.Entity<UserStreak>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_streaks_pkey");

            entity.ToTable("user_streaks");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.CurrentStreak)
                .HasDefaultValue(0)
                .HasColumnName("current_streak");
            entity.Property(e => e.LastActiveDate).HasColumnName("last_active_date");
            entity.Property(e => e.LongestStreak)
                .HasDefaultValue(0)
                .HasColumnName("longest_streak");

            entity.HasOne(d => d.User).WithOne(p => p.UserStreak)
                .HasForeignKey<UserStreak>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_streaks_user_id_fkey");
        });

        modelBuilder.Entity<UserStudyProgress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_study_progress_pkey");

            entity.ToTable("user_study_progress");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CompletedAt)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("completed_at");
            entity.Property(e => e.IsCompleted)
                .HasDefaultValue(false)
                .HasColumnName("is_completed");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.StudyPlanId).HasColumnName("study_plan_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Problem).WithMany(p => p.UserStudyProgresses)
                .HasForeignKey(d => d.ProblemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_study_progress_problem_id_fkey");

            entity.HasOne(d => d.StudyPlan).WithMany(p => p.UserStudyProgresses)
                .HasForeignKey(d => d.StudyPlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_study_progress_study_plan_id_fkey");

            entity.HasOne(d => d.User).WithMany(p => p.UserStudyProgresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_study_progress_user_id_fkey");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("wallet_pkey");

            entity.ToTable("wallet");

            entity.HasIndex(e => new { e.UserId, e.Currency }, "unique_user_currency_wallet").IsUnique();

            entity.Property(e => e.WalletId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("wallet_id");
            entity.Property(e => e.Balance)
                .HasPrecision(18, 2)
                .HasColumnName("balance");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Currency)
                .HasDefaultValueSql("'coin'::text")
                .HasColumnName("currency");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Wallets)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("wallet_user_id_fkey");
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("wallet_transaction_pkey");

            entity.ToTable("wallet_transaction");

            entity.Property(e => e.TransactionId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("transaction_id");
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Direction).HasColumnName("direction");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.SourceId).HasColumnName("source_id");
            entity.Property(e => e.SourceType).HasColumnName("source_type");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'completed'::text")
                .HasColumnName("status");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.WalletId).HasColumnName("wallet_id");

            entity.HasOne(d => d.Wallet).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("wallet_transaction_wallet_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
