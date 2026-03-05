namespace WebAPI.Controllers.v1.SubjectManagement;

// ── Subject Responses ─────────────────────────────────────

public record SubjectResponse(
    Guid SubjectId,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt);

public record SubjectListResponse(
    List<SubjectResponse> Items,
    int TotalCount);
