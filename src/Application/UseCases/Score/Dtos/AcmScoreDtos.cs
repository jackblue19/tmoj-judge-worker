namespace Application.UseCases.Score.Dtos;

public sealed record AcmSubmissionEntryDto(
    Guid Id,
    string? Verdict,
    DateTime SubmittedAt);

public sealed record AcmProblemResultDto(
    bool Solved,
    int Score,
    int WrongAttempts,
    int PenaltyMinutes,
    DateTime? FirstAcAt,
    int TotalSubmissions,
    List<AcmSubmissionEntryDto> SubmissionHistory);

public sealed record AcmProblemScoreDto(
    Guid ContestProblemId,
    Guid TeamId,
    string ScoringMode,
    string PenaltyFormula,
    bool Solved,
    int Score,
    int WrongAttempts,
    int PenaltyMinutes,
    DateTime? FirstAcAt,
    int TotalSubmissions,
    List<AcmSubmissionEntryDto> SubmissionHistory);

public sealed record AcmContestProblemEntryDto(
    Guid ContestProblemId,
    string? Alias,
    int? Ordinal,
    bool Solved,
    int Score,
    int WrongAttempts,
    int PenaltyMinutes,
    DateTime? FirstAcAt,
    int TotalSubmissions);

public sealed record AcmContestScoreDto(
    Guid ContestId,
    Guid TeamId,
    string ScoringMode,
    int TotalScore,
    int TotalPenalty,
    string PenaltyFormula,
    int SolvedCount,
    int TotalProblems,
    List<AcmContestProblemEntryDto> Problems);
