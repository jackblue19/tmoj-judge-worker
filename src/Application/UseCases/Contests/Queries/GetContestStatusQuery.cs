using Application.UseCases.Contests.Dtos;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetContestStatusQuery : IRequest<ContestStatusDto>
{
    public Guid ContestId { get; set; }
}
