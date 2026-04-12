namespace Application.UseCases.Score.Dtos;

public sealed record IoiCaseDto(
    Guid? TestcaseId,
    int? Ordinal,
    string? Verdict,
    bool Passed,
    int? RuntimeMs,
    int? MemoryKb,
    string? Type);

public sealed record IoiSubmissionScoreDto(
    Guid SubmissionId,
    string ScoringMode,
    string? Verdict,
    string? Note,
    int TotalScore,
    int PassedCases,
    int TotalCases,
    List<IoiCaseDto> Cases);

public sealed record IoiStandaloneProblemScoreDto(
    Guid SubmissionId,
    Guid ProblemId,
    string ScoringMode,
    int TotalScore,
    int PassedCases,
    int TotalCases,
    List<IoiCaseDto> Cases);

public sealed record IoiSubmissionEntryDto(
    Guid SubmissionId,
    DateTime SubmittedAt,
    int TotalScore,
    int PassedCases,
    int TotalCases);

public sealed record IoiBestSubmissionDetailDto(
    int PassedCases,
    int TotalCases,
    List<IoiCaseDto> Cases);

public sealed record IoiProblemScoreDto(
    Guid ContestProblemId,
    Guid TeamId,
    string ScoringMode,
    int BestScore,
    int TotalSubmissions,
    Guid? BestSubmissionId,
    IoiBestSubmissionDetailDto? BestSubmissionDetail,
    List<IoiSubmissionEntryDto> Submissions);
