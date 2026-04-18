using Application.Common.Interfaces;
using Application.Common.Models;
using Application.UseCases.Contests.Dtos;
using MediatR;

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
        // =========================
        // NORMALIZE INPUT
        // =========================
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

        if (pageSize > 100)
            pageSize = 100;

        var status = request.Status?.Trim().ToLower();

        // =========================
        // CALL REPO
        // =========================
        return await _repo.GetContestsAsync(
            status,
            page,
            pageSize
        );
    }
}