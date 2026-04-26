using Application.Common.Interfaces;
using Application.UseCases.ProblemTemplates.Dtos;
using Application.UseCases.ProblemTemplates.Specifications;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.ProblemTemplates.Queries.GetProblemTemplatesByProblemId;

public sealed class GetProblemTemplatesByProblemIdQueryHandler
    : IRequestHandler<GetProblemTemplatesByProblemIdQuery , IReadOnlyList<ProblemTemplateDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IReadRepository<ProblemTemplate , Guid> _problemTemplateReadRepository;

    public GetProblemTemplatesByProblemIdQueryHandler(
        ICurrentUserService currentUser ,
        IReadRepository<ProblemTemplate , Guid> problemTemplateReadRepository)
    {
        _currentUser = currentUser;
        _problemTemplateReadRepository = problemTemplateReadRepository;
    }

    public async Task<IReadOnlyList<ProblemTemplateDto>> Handle(
        GetProblemTemplatesByProblemIdQuery request ,
        CancellationToken ct)
    {
        var publicResult = await _problemTemplateReadRepository.ListAsync(
            new PublicProblemTemplatesByProblemIdSpec(request.ProblemId) ,
            ct);

        if ( publicResult.Count > 0 )
            return publicResult;

        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        var currentUserId = _currentUser.UserId.Value;

        var isAdmin =
            _currentUser.IsInRole("admin") ||
            _currentUser.IsInRole("teacher") ||
            _currentUser.IsInRole("manager");

        var managementResult = await _problemTemplateReadRepository.ListAsync(
            new ManagementProblemTemplatesByProblemIdSpec(
                request.ProblemId ,
                currentUserId ,
                isAdmin) ,
            ct);

        if ( managementResult.Count == 0 )
            throw new KeyNotFoundException("Problem templates not found or access denied.");

        return managementResult;
    }
}