namespace WebAPI.Controllers.v1.ClassMemberManagement;

/// <summary>Request to add a user to a class.</summary>
public record CreateClassMemberRequest(
    Guid? ClassId,
    Guid? UserId,
    string? Email,
    bool IsActive = true);

/// <summary>Request to update class member details.</summary>
public record UpdateClassMemberRequest(
    bool IsActive);
