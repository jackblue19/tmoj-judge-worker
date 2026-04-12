namespace Application.UseCases.Score.Dtos;

public sealed record InspectSubmissionDto(
    InspectSubmissionInfoDto Submission,
    int ResultCount,
    List<InspectResultDto> Results,
    int JudgeRunCount,
    List<InspectJudgeRunDto> JudgeRuns,
    List<InspectJudgeJobDto> JudgeJobs);

public sealed record InspectSubmissionInfoDto(
    Guid Id,
    string? StatusCode,
    string? VerdictCode,
    decimal? FinalScore,
    DateTime? JudgedAt,
    Guid? TestsetId,
    Guid ProblemId,
    Guid? ContestProblemId);

public sealed record InspectResultDto(
    Guid Id,
    Guid? TestcaseId,
    string? StatusCode,
    string? Type,
    int? RuntimeMs,
    int? MemoryKb,
    string? Message,
    Guid? JudgeRunId);

public sealed record InspectJudgeRunDto(
    Guid Id,
    string? Status,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    int? CompileExitCode,
    int? CompileTimeMs,
    int? TotalTimeMs,
    int? TotalMemoryKb,
    string? Note);

public sealed record InspectJudgeJobDto(
    Guid Id,
    string? Status,
    string? LastError,
    DateTime EnqueueAt);
