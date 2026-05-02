using Application.UseCases.ProblemSolved.Dtos;
using MediatR;

namespace Application.UseCases.ProblemSolved.Queries;

public sealed record GetMyProblemSolvedStatsQuery(
    string? VisibilityCode ,
    string? SolvedSourceCode
) : IRequest<ProblemSolvedStatsDto>;