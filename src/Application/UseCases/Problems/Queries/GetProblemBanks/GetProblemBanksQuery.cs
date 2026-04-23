using Application.Common.Pagination;
using MediatR;

namespace Application.UseCases.Problems.Queries.GetProblemBanks;

public sealed record GetProblemBanksQuery(
    int Page = 1 ,
    int PageSize = 20 ,
    string? Search = null ,
    string? Difficulty = null
) : IRequest<ApiPagedResponse<ProblemBankListItemDto>>;