using MediatR;

namespace Application.UseCases.Contests.Commands;

public record PublishContestCommand(Guid ContestId) : IRequest<Guid>;