using MediatR;
using Application.UseCases.Contests.Dtos;

namespace Application.UseCases.Contests.Commands;

public record PublishContestCommand(Guid ContestId)
    : IRequest<PublishContestResultDto>;