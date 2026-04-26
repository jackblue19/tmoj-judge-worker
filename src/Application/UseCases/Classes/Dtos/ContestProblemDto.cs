namespace Application.UseCases.Classes.Dtos;

public record ContestProblemDto(
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
