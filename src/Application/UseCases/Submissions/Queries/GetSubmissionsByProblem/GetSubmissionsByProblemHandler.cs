using Application.Common.Pagination;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.UseCases.Submissions.Queries.GetSubmissionsByProblem;

public sealed class GetSubmissionsByProblemHandler
    : IRequestHandler<GetSubmissionsByProblemQuery , ApiPagedResponse<SubmissionListItemDto>>
{
    private readonly IReadRepository<Submission , Guid> _repo;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetSubmissionsByProblemHandler(
        IReadRepository<Submission , Guid> repo ,
        IHttpContextAccessor httpContextAccessor)
    {
        _repo = repo;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ApiPagedResponse<SubmissionListItemDto>> Handle(
        GetSubmissionsByProblemQuery request ,
        CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 1 : request.PageSize;

        var countSpec = new SubmissionsByProblemCountSpec(
            request.ProblemId ,
            request.CurrentUserId ,
            request.IsElevated);

        var listSpec = new SubmissionsByProblemPagedSpec(
            request.ProblemId ,
            request.CurrentUserId ,
            request.IsElevated ,
            page ,
            pageSize);

        var result = await _repo.PageAsync(
            countSpec ,
            listSpec ,
            page ,
            pageSize ,
            ct);

        var totalPages = result.TotalCount == 0
            ? 0
            : (long) Math.Ceiling(result.TotalCount / (double) pageSize);

        var pagination = new PaginationMeta(
            page ,
            pageSize ,
            result.TotalCount ,
            totalPages ,
            page > 1 ,
            page < totalPages
        );

        return ApiPagedResponse<SubmissionListItemDto>.Ok(
            result.Items ,
            pagination ,
            "Fetched submissions by problem." ,
            _httpContextAccessor.HttpContext?.TraceIdentifier
        );
    }
}