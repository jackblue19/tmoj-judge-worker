using Application.Common.Pagination;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Queries.GetPublicProblems;

public sealed class GetPublicProblemsHandler
    : IRequestHandler<GetPublicProblemsQuery , ApiPagedResponse<PublicProblemListItemDto>>
{
    private readonly IReadRepository<Problem , Guid> _problemReadRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetPublicProblemsHandler(
        IReadRepository<Problem , Guid> problemReadRepository ,
        IHttpContextAccessor httpContextAccessor)
    {
        _problemReadRepository = problemReadRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ApiPagedResponse<PublicProblemListItemDto>> Handle(
        GetPublicProblemsQuery request ,
        CancellationToken ct)
    {
        var safePage = request.Page < 1 ? 1 : request.Page;
        var safePageSize = request.PageSize < 1 ? 1 : request.PageSize;

        var countSpec = new PublicProblemsCountSpec(
            request.Search ,
            request.Difficulty);

        var listSpec = new PublicProblemsPagedSpec(
            safePage ,
            safePageSize ,
            request.Search ,
            request.Difficulty);

        var pagedResult = await _problemReadRepository.PageAsync(
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

        return ApiPagedResponse<PublicProblemListItemDto>.Ok(
            data: pagedResult.Items ,
            pagination: pagination ,
            message: "Fetched public problems successfully." ,
            traceId: _httpContextAccessor.HttpContext?.TraceIdentifier
        );
    }
}