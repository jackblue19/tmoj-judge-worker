using Application.Common.Interfaces;
using Application.UseCases.Problems.Constants;
using Application.UseCases.Problems.Dtos;
using Application.UseCases.Problems.Specifications;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Problems.Queries.GetProblemById;

public sealed class GetProblemDetailQueryHandler
    : IRequestHandler<GetProblemDetailQuery , ProblemDetailDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IReadRepository<Problem , Guid> _readRepository;

    public GetProblemDetailQueryHandler(
        ICurrentUserService currentUser ,
        IReadRepository<Problem , Guid> readRepository)
    {
        _currentUser = currentUser;
        _readRepository = readRepository;
    }

    public async Task<ProblemDetailDto> Handle(
        GetProblemDetailQuery request ,
        CancellationToken ct)
    {
        var publicResult = await _readRepository.FirstOrDefaultAsync(
            new ProblemPublicDetailSpec(request.ProblemId) , ct);

        if ( publicResult is not null )
            return publicResult;

        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        var currentUserId = _currentUser.UserId.Value;

        var isAdmin = Roles.AdminRoles
            .Any(role => _currentUser.IsInRole(role) || _currentUser.IsInRole("student"));

        var managementResult = await _readRepository.FirstOrDefaultAsync(
            new ProblemManagementDetailSpec(
                request.ProblemId ,
                currentUserId ,
                isAdmin) ,
            ct);

        if ( managementResult is null )
            throw new KeyNotFoundException("Problem not found or access denied.");

        return managementResult;
    }
}