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
