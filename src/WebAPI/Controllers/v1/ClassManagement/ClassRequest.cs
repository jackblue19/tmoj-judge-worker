namespace WebAPI.Controllers.v1.ClassManagement;

// ── Class Requests ────────────────────────────────────────

/// <summary>Create a new class under a subject & semester (Manager).</summary>
public record CreateClassRequest(
    Guid SubjectId,
    Guid SemesterId,
    string ClassCode,
    string? Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    Guid? TeacherId);

/// <summary>Update class details (Manager/Teacher).</summary>
public record UpdateClassRequest(
    string? Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    bool? IsActive);

/// <summary>Assign a teacher to a class (Manager).</summary>
public record AssignTeacherRequest(Guid TeacherId);

/// <summary>Promote a user to the teacher role (Manager).</summary>
public record AssignTeacherRoleRequest(Guid UserId);

/// <summary>Add a student to a class by userId or email (Teacher).</summary>
public record AddStudentRequest(
    Guid? UserId,
    string? Email);

/// <summary>Student joins a class via invite code.</summary>
public record JoinByCodeRequest(string InviteCode);
