using Application.Common.Interfaces;
using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Specifications;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Problems.Queries.GetProblemById;

public sealed class GetProblemDetailQueryHandler : IRequestHandler<GetProblemDetailQuery , ProblemDetailDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProblemRepository _problemRepository;
    private readonly IReadRepository<Problem , Guid> _problemReadRepository;

    public GetProblemDetailQueryHandler(
        ICurrentUserService currentUser ,
        IProblemRepository problemRepository ,
        IReadRepository<Problem , Guid> problemReadRepository)
    {
        _currentUser = currentUser;
        _problemRepository = problemRepository;
        _problemReadRepository = problemReadRepository;
    }

    public async Task<ProblemDetailDto> Handle(GetProblemDetailQuery request , CancellationToken ct)
    {
        var problem = await _problemReadRepository.GetByIdAsync(request.ProblemId , ct);

        if ( problem is null )
            throw new KeyNotFoundException("Problem not found.");

        var isPublic =
            problem.IsActive &&
            problem.StatusCode == "published" &&
            problem.VisibilityCode == "public";

        if ( isPublic )
        {
            var publicDetail = await _problemReadRepository.FirstOrDefaultAsync(
                new ProblemDetailSpec(request.ProblemId) , ct);

            if ( publicDetail is null )
                throw new KeyNotFoundException("Problem not found.");

            return publicDetail;
        }

        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        var currentUserId = _currentUser.UserId.Value;
        var isAdmin =
            _currentUser.IsInRole("Admin") ||
            _currentUser.IsInRole("admin");

        var detail = await _problemRepository.GetProblemDetailForManagementAsync(
            request.ProblemId ,
            currentUserId ,
            isAdmin ,
            ct);

        if ( detail is null )
            throw new KeyNotFoundException("Problem not found or access denied.");

        return detail;
    }
}