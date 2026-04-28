using MediatR;

namespace Application.UseCases.Classes.Commands.AddClassContestProblem;

public record AddClassContestProblemCommand(
    Guid ClassSemesterId,
    Guid ContestId,
    Guid CreatedBy,
    Guid ProblemId,
    string? Alias,
    int? Ordinal,
    int? Points,
    int? MaxScore,
    int? TimeLimitMs,
    int? MemoryLimitKb
) : IRequest<Guid>;
