using Application.Common.Interfaces;
using Application.Common.Models;
using Application.UseCases.Contests.Dtos;
using MediatR;
using Application.Common.Helpers;

namespace Application.UseCases.Contests.Queries;

public class GetContestsQueryHandler
    : IRequestHandler<GetContestsQuery, PagedResult<ContestDto>>
{
    private readonly IContestRepository _repo;

    public GetContestsQueryHandler(IContestRepository repo)
    {
        _repo = repo;
    }

    public async Task<PagedResult<ContestDto>> Handle(
        GetContestsQuery request,
        CancellationToken ct)
    {
        return await _repo.GetContestsAsync(
            request.Status,
            request.Page,
            request.PageSize
        );
    }
}