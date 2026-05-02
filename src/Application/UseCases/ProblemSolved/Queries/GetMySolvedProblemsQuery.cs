using Application.UseCases.ProblemSolved.Dtos;
using MediatR;

namespace Application.UseCases.ProblemSolved.Queries;

public sealed record GetMySolvedProblemsQuery(
    string? VisibilityCode ,
    string? SolvedSourceCode ,
    int Page = 1 ,
    int PageSize = 20
) : IRequest<ProblemSolvedListDto>;