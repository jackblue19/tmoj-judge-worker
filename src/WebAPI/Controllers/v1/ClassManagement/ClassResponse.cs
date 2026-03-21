namespace WebAPI.Controllers.v1.ClassManagement;

// ── Class Responses ───────────────────────────────────────

public record ClassResponse(
    Guid ClassId,
    string ClassCode,
    string ClassName,
    string? Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool IsActive,
    string? InviteCode,
    DateTime? InviteCodeExpiresAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    // nested info
    ClassSubjectInfo Subject,
    ClassSemesterInfo Semester,
    ClassTeacherInfo? Teacher,
    int MemberCount);

public record ClassSubjectInfo(
    Guid SubjectId,
    string Code,
    string Name);

public record ClassSemesterInfo(
    Guid SemesterId,
    string Code,
    string Name);

public record ClassTeacherInfo(
    Guid UserId,
    string? DisplayName,
    string? Email,
    string? AvatarUrl);

public record ClassListResponse(
    List<ClassResponse> Items,
    int TotalCount);

/// <summary>Returned after generating an invite code.</summary>
public record InviteCodeResponse(
    Guid ClassId,
    string InviteCode,
    DateTime ExpiresAt);

/// <summary>Student information within a class (Teacher view).</summary>
public record ClassMemberResponse(
    Guid UserId,
    string? DisplayName,
    string? Email,
    string? AvatarUrl,
    DateTime JoinedAt,
    bool IsActive);

/// <summary>Detailed student info with class-specific stats.</summary>
public record StudentInfoResponse(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? Email,
    string? AvatarUrl,
    DateTime JoinedAt,
    bool IsActive,
    int SubmissionCount,
    int SolvedCount,
    DateTime? LastSubmissionAt);

/// <summary>Ranking entry for a student in a class.</summary>
public record ClassRankingEntry(
    int Rank,
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    int SolvedCount,
    decimal TotalScore,
    int SubmissionCount);
