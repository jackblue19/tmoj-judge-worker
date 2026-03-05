namespace WebAPI.Controllers.v1.SubjectManagement;

// ── Subject Requests ──────────────────────────────────────

/// <summary>Create a new subject (Manager).</summary>
public record CreateSubjectRequest(
    string Code,
    string Name,
    string? Description);

/// <summary>Update an existing subject (Manager).</summary>
public record UpdateSubjectRequest(
    string? Code,
    string? Name,
    string? Description,
    bool? IsActive);
