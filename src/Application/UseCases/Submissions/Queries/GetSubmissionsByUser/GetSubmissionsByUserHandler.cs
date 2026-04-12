using Application.Common.Pagination;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.UseCases.Submissions.Queries.GetSubmissionsByUser;

public sealed class GetSubmissionsByUserHandler
    : IRequestHandler<GetSubmissionsByUserQuery , ApiPagedResponse<SubmissionByUserListItemDto>>
{
    private readonly IReadRepository<Submission , Guid> _submissionReadRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetSubmissionsByUserHandler(
        IReadRepository<Submission , Guid> submissionReadRepository ,
        IHttpContextAccessor httpContextAccessor)
    {
        _submissionReadRepository = submissionReadRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ApiPagedResponse<SubmissionByUserListItemDto>> Handle(
        GetSubmissionsByUserQuery request ,
        CancellationToken ct)
    {
        if ( !request.IsElevated && request.UserId != request.CurrentUserId )
            throw new UnauthorizedAccessException("You are not allowed to view submissions of another user.");

        var safePage = request.Page < 1 ? 1 : request.Page;
        var safePageSize = request.PageSize < 1 ? 1 : request.PageSize;

        var countSpec = new SubmissionsByUserCountSpec(request.UserId);

        var listSpec = new SubmissionsByUserPagedSpec(
            request.UserId ,
            safePage ,
            safePageSize);

        var pagedResult = await _submissionReadRepository.PageAsync(
            countSpec ,
            listSpec ,
            safePage ,
            safePageSize ,
            ct);

        var totalPages = pagedResult.TotalCount == 0
            ? 0
            : (long) Math.Ceiling(pagedResult.TotalCount / (double) safePageSize);

        var pagination = new PaginationMeta(
            Page: safePage ,
            PageSize: safePageSize ,
            TotalCount: pagedResult.TotalCount ,
            TotalPages: totalPages ,
            HasPrevious: safePage > 1 ,
            HasNext: safePage < totalPages
        );

        return ApiPagedResponse<SubmissionByUserListItemDto>.Ok(
            data: pagedResult.Items ,
            pagination: pagination ,
            message: "Fetched submissions by user successfully." ,
            traceId: _httpContextAccessor.HttpContext?.TraceIdentifier
        );
    }
}