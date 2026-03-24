using Application.UseCases.Problems.Mappings;
using Domain.Abstractions;
using MediatR;

namespace Application.UseCases.Problems.Queries.GetAllProblems;

public sealed record GetProblemsQuery(
    string? Difficulty,
    string? Status,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<ProblemListItemDto>>;