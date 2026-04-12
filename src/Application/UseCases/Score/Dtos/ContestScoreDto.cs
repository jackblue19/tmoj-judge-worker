namespace Application.UseCases.Score.Dtos;

/// <summary>
/// DTO tổng hợp cho contest scoring (có thể là IOI hoặc ACM).
/// Khi ScoringMode = "ioi": dùng các field IOI, các field ACM null.
/// Khi ScoringMode = "acm": dùng các field ACM, các field IOI null.
/// </summary>
public sealed record ContestScoreDto(
    Guid ContestId,
    Guid TeamId,
    string ScoringMode,
    int TotalScore,
    int TotalProblems,
    int? TotalPenalty,
    string? PenaltyFormula,
    int? SolvedCount,
    List<ContestProblemScoreDto> Problems);

public sealed record ContestProblemScoreDto(
    Guid ContestProblemId,
    string? Alias,
    int? Ordinal,
    // IOI fields
    int? BestScore,
    Guid? BestSubmissionId,
    int? PassedCases,
    int? TotalCases,
    // ACM fields
    bool? Solved,
    int? Score,
    int? WrongAttempts,
    int? PenaltyMinutes,
    DateTime? FirstAcAt,
    int? TotalSubmissions);
