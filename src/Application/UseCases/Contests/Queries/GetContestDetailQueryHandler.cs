using Application.UseCases.Contests.Dtos;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Contests.Queries;

// ⚠️ IMPORTANT: handler này giờ handle luôn GetContestDetailQuery
public class GetContestProblemsQueryHandler
    : IRequestHandler<GetContestDetailQuery, ContestDetailDto>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;

    public GetContestProblemsQueryHandler(
        IReadRepository<Contest, Guid> contestRepo)
    {
        _contestRepo = contestRepo;
    }

    public async Task<ContestDetailDto> Handle(
        GetContestDetailQuery request,
        CancellationToken ct)
    {
        var contest = await _contestRepo.FirstOrDefaultAsync(
            new GetContestDetailSpec(request.ContestId),
            ct);

        if (contest == null)
            throw new Exception("Contest not found");

        var now = DateTime.UtcNow;

        string status =
            now < contest.StartAt ? "upcoming" :
            now > contest.EndAt ? "ended" :
            "running";

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

            Problems = contest.ContestProblems!
                .Where(cp => cp.IsActive)
                .OrderBy(cp => cp.DisplayIndex ?? cp.Ordinal ?? 999)
                .ThenBy(cp => cp.Alias)
                .Select(cp => new ContestProblemDto
                {
                    Id = cp.Id,
                    ProblemId = cp.ProblemId,
                    Title = cp.Problem.Title,
                    Alias = cp.Alias,
                    Points = cp.Points ?? 0
                })
                .ToList()
        };
    }
}