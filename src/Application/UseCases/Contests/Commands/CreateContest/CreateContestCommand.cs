using MediatR;

namespace Application.UseCases.Contests.Commands;

public record CreateContestCommand(
    string Title,
    string? Description,
    DateTime StartAt,
    DateTime EndAt,
    string VisibilityCode,
    bool AllowTeams,
    string? ContestType
) : IRequest<Guid>;