using Application.Common.Models;
using Application.UseCases.Contests.Dtos;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions; 
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetContestsQueryHandler
    : IRequestHandler<
        GetContestsQuery,
        Application.Common.Models.PagedResult<ContestDto> 
    >
{
    private readonly IReadRepository<Domain.Entities.Contest, Guid> _repo;

    public GetContestsQueryHandler(
        IReadRepository<Domain.Entities.Contest, Guid> repo)
    {
        _repo = repo;
    }

    public async Task<Application.Common.Models.PagedResult<ContestDto>> Handle(
        GetContestsQuery request,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var spec = new GetContestsSpec(
            request.Status,
            1,
            int.MaxValue
        );

        var allContests = await _repo.ListAsync(spec, ct);

        var sorted = allContests
            .Select(x => new
            {
                Contest = x,
                Priority =
                    x.StartAt <= now && x.EndAt >= now ? 0 :
                    x.StartAt > now ? 1 :
                    2
            })
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Contest.StartAt)
            .Select(x => x.Contest)
            .ToList();

        var total = sorted.Count;

        var paged = sorted
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var items = paged.Select(x => new ContestDto
        {
            Id = x.Id,
            Title = x.Title,
            StartAt = x.StartAt,
            EndAt = x.EndAt,
            VisibilityCode = x.VisibilityCode,
            ContestType = x.ContestType,
            AllowTeams = x.AllowTeams,
            Status =
                x.StartAt > now ? "upcoming" :
                x.EndAt < now ? "ended" :
                "running"
        }).ToList();

        return new Application.Common.Models.PagedResult<ContestDto>
        {
            Items = items,
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}