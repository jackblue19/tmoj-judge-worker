using Application.UseCases.Contests.Dtos;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetContestProblemsQuery : IRequest<List<ContestProblemDto>>
{
    public Guid ContestId { get; set; }

    public GetContestProblemsQuery(Guid contestId)
    {
        ContestId = contestId;
    }
}
