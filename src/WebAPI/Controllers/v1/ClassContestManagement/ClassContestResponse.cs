namespace WebAPI.Controllers.v1.ClassContestManagement;

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
