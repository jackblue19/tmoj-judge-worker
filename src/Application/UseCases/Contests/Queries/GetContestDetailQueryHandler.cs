using Application.Common.Interfaces;
using Application.UseCases.Contests.Dtos;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetContestDetailQueryHandler
    : IRequestHandler<GetContestDetailQuery, ContestDetailDto>
{
    private readonly IReadRepository<Domain.Entities.Contest, Guid> _repo;

    public GetContestDetailQueryHandler(
        Domain.Abstractions.IReadRepository<Domain.Entities.Contest, Guid> repo)
    {
        _repo = repo;
    }

    public async Task<ContestDetailDto> Handle(
        GetContestDetailQuery request,
        CancellationToken ct)
    {
        var spec = new GetContestDetailSpec(request.ContestId);

        var contest = await _repo.FirstOrDefaultAsync(spec, ct);

        if (contest == null)
            throw new Exception("Contest not found");

        var now = DateTime.UtcNow;

        var status =
            contest.StartAt > now ? "upcoming" :
            contest.EndAt < now ? "ended" :
            "running";

        var problems = contest.ContestProblems?
            .OrderBy(x => x.Ordinal)
            .Select(x => new ContestProblemDto
            {
                Id = x.Id,
                ProblemId = x.ProblemId,
                Title = x.Problem?.Title ?? "Unknown",

                Alias = x.Alias,
                Points = x.Points
            })
            .ToList() ?? new List<ContestProblemDto>();

        return new ContestDetailDto
        {
            Id = contest.Id,
            Title = contest.Title,
            Description = contest.DescriptionMd,

            StartAt = contest.StartAt,
            EndAt = contest.EndAt,

            VisibilityCode = contest.VisibilityCode,
            ContestType = contest.ContestType,
            AllowTeams = contest.AllowTeams,

            Status = status,
            Problems = problems
        };
    }
}