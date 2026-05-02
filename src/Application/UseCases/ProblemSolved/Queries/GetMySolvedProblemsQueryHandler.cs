using Application.Common.Interfaces;
using Application.UseCases.ProblemSolved.Dtos;
using MediatR;

namespace Application.UseCases.ProblemSolved.Queries;

public sealed class GetMySolvedProblemsQueryHandler
    : IRequestHandler<GetMySolvedProblemsQuery , ProblemSolvedListDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProblemSolvedQueryService _problemSolvedQueryService;

    public GetMySolvedProblemsQueryHandler(
        ICurrentUserService currentUser ,
        IProblemSolvedQueryService problemSolvedQueryService)
    {
        _currentUser = currentUser;
        _problemSolvedQueryService = problemSolvedQueryService;
    }

    public async Task<ProblemSolvedListDto> Handle(
        GetMySolvedProblemsQuery request ,
        CancellationToken cancellationToken)
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        return await _problemSolvedQueryService.GetSolvedProblemsAsync(
            _currentUser.UserId.Value ,
            request.VisibilityCode ,
            request.SolvedSourceCode ,
            request.Page ,
            request.PageSize ,
            cancellationToken);
    }
}