using Application.UseCases.Contests.Dtos;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Ardalis.Specification;

namespace Application.UseCases.Contests.Queries;

public class GetContestProblemsQueryHandler
    : IRequestHandler<GetContestProblemsQuery, List<ContestProblemDto>>
{
    private readonly IReadRepository<ContestProblem, Guid> _repo;

    public GetContestProblemsQueryHandler(
        IReadRepository<ContestProblem, Guid> repo)
    {
        _repo = repo;
    }

    public async Task<List<ContestProblemDto>> Handle(
        GetContestProblemsQuery request,
        CancellationToken ct)
    {
        var data = await _repo.ListAsync(
            new ContestProblemByContestSpec(request.ContestId),
            ct);

        return data
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayIndex ?? x.Ordinal ?? 999)
            .ThenBy(x => x.Alias)
            .Select(x => new ContestProblemDto
            {
                Id = x.Id,
                ProblemId = x.ProblemId,
                Title = x.Problem.Title,
                Alias = x.Alias,
                Points = x.Points ?? 0
            })
            .ToList();
    }
}