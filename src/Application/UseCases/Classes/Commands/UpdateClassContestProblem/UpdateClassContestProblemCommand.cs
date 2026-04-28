using MediatR;

namespace Application.UseCases.Classes.Commands.UpdateClassContestProblem;

public record UpdateClassContestProblemCommand(
    Guid ClassSemesterId,
    Guid ContestId,
    Guid ContestProblemId,
    string? Alias,
    int? Ordinal,
    int? Points,
    int? MaxScore,
    int? TimeLimitMs,
    int? MemoryLimitKb
) : IRequest;
