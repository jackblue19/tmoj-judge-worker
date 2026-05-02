using Application.UseCases.ProblemSolved.Dtos;

namespace Application.Common.Interfaces;

public interface IProblemSolvedQueryService
{
    Task<ProblemSolvedStatsDto> GetSolvedStatsAsync(
        Guid userId ,
        string? visibilityCode ,
        string? solvedSourceCode ,
        CancellationToken cancellationToken = default);
}