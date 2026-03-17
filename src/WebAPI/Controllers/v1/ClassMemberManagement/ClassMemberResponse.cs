namespace WebAPI.Controllers.v1.ClassMemberManagement;

/// <summary>Basic information for a class member.</summary>
public record ClassMemberResponse(
    Guid Id,
    Guid ClassId,
    Guid UserId,
    string? UserDisplayName,
    string? UserEmail,
    string? UserAvatarUrl,
    DateTime JoinedAt,
    bool IsActive);

/// <summary>Paginated list of class members.</summary>
public record ClassMemberListResponse(
    List<ClassMemberResponse> Items,
    int TotalCount);

/// <summary>Result of importing students from Excel.</summary>
public record ImportResultResponse(
    int TotalProcessed,
    int SuccessCount,
    int FailedCount,
    List<string> Errors);
