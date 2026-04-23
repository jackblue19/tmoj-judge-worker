using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Gamification.Queries.GetAllBadges;

public class GetAllBadgesHandler : IRequestHandler<GetAllBadgesQuery, List<Badge>>
{
    private readonly IGamificationRepository _repo;

    public GetAllBadgesHandler(IGamificationRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<Badge>> Handle(
        GetAllBadgesQuery request,
        CancellationToken cancellationToken)
    {
        return await _repo.GetAllBadgesAsync();
    }
}
