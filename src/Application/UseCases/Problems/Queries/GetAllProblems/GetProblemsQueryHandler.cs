using Application.UseCases.Problems.Mappings;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Queries.GetAllProblems;

public sealed class GetProblemsQueryHandler
    : IRequestHandler<GetProblemsQuery , PagedResult<ProblemListItemDto>>
{
    private readonly IReadRepository<Problem , Guid> _repo;

    public GetProblemsQueryHandler(
        IReadRepository<Problem , Guid> repo)
    {
        _repo = repo;
    }

    public async Task<PagedResult<ProblemListItemDto>> Handle(
        GetProblemsQuery request ,
        CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        var skip = (page - 1) * pageSize;

        var listSpec = new ProblemsPagingSpec(
            request.Difficulty ,
            request.Status ,
            skip ,
            pageSize);

        var countSpec = new ProblemsCountSpec(
            request.Difficulty ,
            request.Status);

        return await _repo.PageAsync(
            countSpec ,
            listSpec ,
            page ,
            pageSize ,
            ct);
    }
}
