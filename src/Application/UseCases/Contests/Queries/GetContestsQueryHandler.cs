using Application.Common.Interfaces;
using Application.Common.Models;
using Application.UseCases.Contests.Dtos;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetContestsQueryHandler
    : IRequestHandler<GetContestsQuery, PagedResult<ContestDto>>
{
    private readonly IContestRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public GetContestsQueryHandler(
        IContestRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
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
        var visibility = request.VisibilityCode?.Trim().ToLower();

        var isPrivileged =
            _currentUser.IsAuthenticated &&
            (_currentUser.IsInRole("admin") || _currentUser.IsInRole("manager"));

        // Non-admins can only query public contests
        if (!isPrivileged && !string.IsNullOrEmpty(visibility) && visibility != "public")
            throw new UnauthorizedAccessException("NO_PERMISSION");

        // Archived contests (IsActive=false) are visible only to admin/manager
        var includeArchived = isPrivileged;

        // =========================
        // CALL REPO
        // =========================
        return await _repo.GetContestsAsync(
            status,
            visibility,
            includeArchived,
            page,
            pageSize
        );
    }
}