using Application.Common.Pagination;
using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Queries.GetProblemBanks;
using MediatR;

namespace Application.UseCases.Problems.Queries.GetInPlanProblems;

public sealed record GetInPlanProblemsQuery(
    int Page = 1 ,
    int PageSize = 20 ,
    string? Search = null ,
    string? Difficulty = null
) : IRequest<ApiPagedResponse<ProblemBankListItemDto>>;
