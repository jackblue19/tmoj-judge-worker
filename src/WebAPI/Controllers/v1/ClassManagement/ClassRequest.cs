namespace WebAPI.Controllers.v1.ClassManagement;

// ── Class Requests ────────────────────────────────────────

/// <summary>Create a new class under a subject & semester (Manager).</summary>
public record CreateClassRequest(
    Guid SubjectId,
    Guid SemesterId,
    string ClassCode,
    Guid? TeacherId);

/// <summary>Update class details (Manager/Teacher).</summary>
public record UpdateClassRequest(
    bool? IsActive);

/// <summary>Assign a teacher to a class (Manager).</summary>
public record AssignTeacherRequest(Guid TeacherId);

/// <summary>Promote a user to the teacher role (Manager).</summary>
public record AssignTeacherRoleRequest(Guid UserId);

/// <summary>Add a student to a class by userId or email (Teacher).</summary>
public record AddStudentRequest(
    Guid? UserId,
    string? Email,
    Guid SemesterId,
    Guid SubjectId);

/// <summary>Update class member status (Teacher/Manager).</summary>
public record UpdateClassMemberStatusRequest(bool IsActive);

/// <summary>Student joins a class via invite code.</summary>
public record JoinByCodeRequest(string InviteCode);

/// <summary>Link a class to a semester (Manager).</summary>
public record AddClassSemesterRequest(
    Guid SemesterId,
    Guid SubjectId,
    Guid? TeacherId);

/// <summary>Update a class-semester instance (Manager).</summary>
public record UpdateClassSemesterRequest(
    Guid? ClassId,
    Guid? SemesterId,
    Guid? SubjectId,
    Guid? TeacherId);

/// <summary>Generate an invite code (Teacher).</summary>
public record GenerateInviteCodeRequest(int MinutesValid = 15);

/// <summary>Teacher manually adds a student.</summary>
public record AddStudentManuallyRequest(string? RollNumber, string? MemberCode);

// ── Class Contest Requests ────────────────────────────────

/// <summary>Create a contest scoped to a class (Teacher).</summary>
public record CreateClassContestRequest(
    string Title,
    string? Slug,
    string? DescriptionMd,
    DateTime StartAt,
    DateTime EndAt,
    DateTime? FreezeAt,
    string? Rules,
    string? ScoringCode,
    List<ContestProblemItem>? Problems,
    // slot metadata
    int? SlotNo,
    string? SlotTitle);

/// <summary>Problem entry for a class contest.</summary>
public record ContestProblemItem(
    Guid ProblemId,
    int? Ordinal,
    string? Alias,
    int? Points,
    int? MaxScore,
    int? TimeLimitMs,
    int? MemoryLimitKb);

/// <summary>Extend the end time of an ongoing contest (Teacher).</summary>
public record ExtendContestRequest(DateTime NewEndAt);