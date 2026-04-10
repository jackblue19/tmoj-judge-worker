namespace WebAPI.Controllers.v1.ClassManagement;

// ── Class Responses ───────────────────────────────────────

public record ClassResponse(
    Guid ClassId ,
    string ClassCode ,
    bool IsActive ,
    DateTime CreatedAt ,
    DateTime UpdatedAt ,
    // nested info
    List<ClassInstanceInfo> Instances ,
    int TotalMemberCount);

public record ClassSubjectInfo(
    Guid SubjectId ,
    string Code ,
    string Name);

public record ClassInstanceInfo(
    Guid ClassSemesterId ,
    Guid SemesterId ,
    string SemesterCode ,
    Guid SubjectId ,
    string SubjectCode ,
    string SubjectName ,
    string? SubjectDescription ,
    DateOnly StartAt ,
    DateOnly EndAt ,
    string? InviteCode ,
    DateTime? InviteCodeExpiresAt ,
    DateTime CreatedAt ,
    ClassTeacherInfo? Teacher ,
    int MemberCount);

public record ClassTeacherInfo(
    Guid UserId ,
    string? DisplayName ,
    string? Email ,
    string? AvatarUrl);

public record ClassListResponse(
    List<ClassResponse> Items ,
    int TotalCount);

/// <summary>Returned after generating an invite code.</summary>
public record InviteCodeResponse(
    Guid InstanceId ,
    string InviteCode ,
    DateTime ExpiresAt);

/// <summary>Student information within a class (Teacher view).</summary>
public record ClassMemberResponse(
    Guid MemberId,
    Guid ClassSemesterId,
    Guid UserId ,
    string? DisplayName ,
    string? Email ,
    string? AvatarUrl ,
    DateTime JoinedAt ,
    bool IsActive);

/// <summary>Detailed student info with class-specific stats.</summary>
public record StudentInfoResponse(
    Guid UserId ,
    string? FirstName ,
    string? LastName ,
    string? DisplayName ,
    string? Email ,
    string? AvatarUrl ,
    DateTime JoinedAt ,
    bool IsActive ,
    int SubmissionCount ,
    int SolvedCount ,
    DateTime? LastSubmissionAt);

/// <summary>Ranking entry for a student in a class.</summary>
public record ClassRankingEntry(
    int Rank ,
    Guid UserId ,
    string? DisplayName ,
    string? AvatarUrl ,
    int SolvedCount ,
    decimal TotalScore ,
    int SubmissionCount);

/// <summary>Result of importing students from Excel.</summary>
public record ImportResultResponse(
    int TotalProcessed,
    int SuccessCount,
    int FailedCount,
    List<string> Errors);

// ── Class Contest Responses ───────────────────────────────

public record ClassContestResponse(
    Guid ContestId,
    Guid ClassId,
    Guid? SlotId,
    string Title,
    string? Slug,
    string? DescriptionMd,
    string? Rules,
    DateTime StartAt,
    DateTime EndAt,
    DateTime? FreezeAt,
    bool IsActive,
    bool IsJoined,
    double? TimeRemainingSeconds,
    DateTime CreatedAt,
    List<ContestProblemResponse> Problems);

public record ContestProblemResponse(
    Guid ContestProblemId,
    Guid ProblemId,
    string? ProblemTitle,
    string? ProblemSlug,
    string? Alias,
    int? Ordinal,
    int? Points,
    int? MaxScore,
    int? TimeLimitMs,
    int? MemoryLimitKb);

/// <summary>Summary card for listing contests in a class.</summary>
public record ClassContestSummaryResponse(
    Guid ContestId,
    string Title,
    string? Slug,
    DateTime StartAt,
    DateTime EndAt,
    bool IsActive,
    int ProblemCount,
    int ParticipantCount);
