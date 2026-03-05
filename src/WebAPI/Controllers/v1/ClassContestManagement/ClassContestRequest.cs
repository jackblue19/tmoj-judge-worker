namespace WebAPI.Controllers.v1.ClassContestManagement;

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
