namespace WebAPI.Controllers.v1.ClassSlotManagement;

// ── ClassSlot (Assignment) Responses ──────────────────────

public record ClassSlotResponse(
    Guid Id,
    Guid ClassId,
    int SlotNo,
    string Title,
    string? Description,
    string? Rules,
    DateTime? OpenAt,
    DateTime? DueAt,
    DateTime? CloseAt,
    string Mode,
    Guid? ContestId,
    bool IsPublished,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<SlotProblemResponse> Problems);

public record SlotProblemResponse(
    Guid ProblemId,
    string? ProblemTitle,
    string? ProblemSlug,
    int? Ordinal,
    int? Points,
    bool IsRequired);

/// <summary>Score overview for one student in a specific slot.</summary>
public record StudentSlotScoreResponse(
    Guid UserId,
    string? DisplayName,
    string? AvatarUrl,
    List<ProblemScoreEntry> ProblemScores,
    decimal TotalScore,
    int SolvedCount);

public record ProblemScoreEntry(
    Guid ProblemId,
    string? ProblemTitle,
    string? VerdictCode,
    decimal? Score,
    int Attempts,
    DateTime? LastSubmittedAt);

/// <summary>Detailed view of a student's submission for a problem in a slot.</summary>
public record StudentSubmissionDetailResponse(
    Guid SubmissionId,
    Guid ProblemId,
    string? ProblemTitle,
    string? VerdictCode,
    decimal? FinalScore,
    int? TimeMs,
    int? MemoryKb,
    string? StatusCode,
    DateTime CreatedAt,
    List<SubmissionResultEntry> Results);

public record SubmissionResultEntry(
    Guid ResultId,
    string? StatusCode,
    int? RuntimeMs,
    int? MemoryKb,
    string? CheckerMessage,
    string? Input,
    string? ExpectedOutput,
    string? ActualOutput);
