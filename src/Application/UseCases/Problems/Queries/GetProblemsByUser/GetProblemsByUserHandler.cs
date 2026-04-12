using Application.Common.Pagination;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.UseCases.Problems.Queries.GetProblemsByUser;

public sealed class GetProblemsByUserHandler
    : IRequestHandler<GetProblemsByUserQuery , ApiPagedResponse<ProblemByUserListItemDto>>
{
    private readonly IUserProblemQueries _userProblemQueries;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetProblemsByUserHandler(
        IUserProblemQueries userProblemQueries ,
        IHttpContextAccessor httpContextAccessor)
    {
        _userProblemQueries = userProblemQueries;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ApiPagedResponse<ProblemByUserListItemDto>> Handle(
        GetProblemsByUserQuery request ,
        CancellationToken ct)
    {
        if ( !request.IsElevated && request.UserId != request.CurrentUserId )
            throw new UnauthorizedAccessException("You are not allowed to view problems of another user.");

        var safePage = request.Page < 1 ? 1 : request.Page;
        var safePageSize = request.PageSize < 1 ? 1 : request.PageSize;

        return await _userProblemQueries.GetProblemsByUserAsync(
            request.UserId ,
            safePage ,
            safePageSize ,
            _httpContextAccessor.HttpContext?.TraceIdentifier ,
            ct);
    }
}