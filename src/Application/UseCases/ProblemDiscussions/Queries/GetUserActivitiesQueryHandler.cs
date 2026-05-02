using Application.Common.Interfaces;
using Application.UseCases.ProblemDiscussions.Dtos;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.ProblemDiscussions.Queries;

public class GetUserActivitiesQueryHandler : IRequestHandler<GetUserActivitiesQuery, List<UserActivityDto>>
{
    private readonly IProblemDiscussionRepository _discussionRepository;

    public GetUserActivitiesQueryHandler(IProblemDiscussionRepository discussionRepository)
    {
        _discussionRepository = discussionRepository;
    }

    public async Task<List<UserActivityDto>> Handle(GetUserActivitiesQuery request, CancellationToken ct)
    {
        return await _discussionRepository.GetUserActivitiesAsync(request.UserId, request.Limit);
    }
}
