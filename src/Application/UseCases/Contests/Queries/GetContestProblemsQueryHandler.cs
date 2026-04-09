using Application.Common.Interfaces;
using Application.UseCases.Contests.Dtos;
using Application.UseCases.Contests.Queries;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

        var result = data
            .Where(x => x.ContestId == request.ContestId && x.IsActive)
            .OrderBy(x => x.DisplayIndex ?? x.Ordinal ?? 999)
            .ThenBy(x => x.Alias)
            .Select(x => new ContestProblemDto
            {
                Id = x.Id,
                ProblemId = x.ProblemId,
                Alias = x.Alias!,
                Ordinal = x.Ordinal,
                DisplayIndex = x.DisplayIndex,
                Points = x.Points ?? 0,
                TimeLimitMs = x.TimeLimitMs,
                MemoryLimitKb = x.MemoryLimitKb,
                Title = x.Problem.Title
            })
            .ToList();

        return result;
    }
}