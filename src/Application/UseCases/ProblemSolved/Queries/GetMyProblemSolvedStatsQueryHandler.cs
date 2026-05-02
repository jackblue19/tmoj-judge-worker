using Application.Common.Interfaces;
using Application.UseCases.ProblemSolved.Dtos;
using MediatR;

namespace Application.UseCases.ProblemSolved.Queries;

public sealed class GetMyProblemSolvedStatsQueryHandler
    : IRequestHandler<GetMyProblemSolvedStatsQuery , ProblemSolvedStatsDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProblemSolvedQueryService _problemSolvedQueryService;

    public GetMyProblemSolvedStatsQueryHandler(
        ICurrentUserService currentUser ,
        IProblemSolvedQueryService problemSolvedQueryService)
    {
        _currentUser = currentUser;
        _problemSolvedQueryService = problemSolvedQueryService;
    }

    public async Task<ProblemSolvedStatsDto> Handle(
        GetMyProblemSolvedStatsQuery request ,
        CancellationToken cancellationToken)
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        return await _problemSolvedQueryService.GetSolvedStatsAsync(
            _currentUser.UserId.Value ,
            request.VisibilityCode ,
            request.SolvedSourceCode ,
            cancellationToken);
    }
}