using Application.Common.Interfaces;
using Application.UseCases.ProblemTemplates.Dtos;
using Application.UseCases.ProblemTemplates.Helpers;
using Application.UseCases.ProblemTemplates.Mappings;
using Application.UseCases.ProblemTemplates.Specifications;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.ProblemTemplates.Commands.UpdateProblemTemplate;

public sealed class UpdateProblemTemplateCommandHandler
    : IRequestHandler<UpdateProblemTemplateCommand , ProblemTemplateDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IReadRepository<ProblemTemplate , Guid> _problemTemplateReadRepository;
    private readonly IWriteRepository<ProblemTemplate , Guid> _problemTemplateWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProblemTemplateCommandHandler> _logger;

    public UpdateProblemTemplateCommandHandler(
        ICurrentUserService currentUser ,
        IReadRepository<ProblemTemplate , Guid> problemTemplateReadRepository ,
        IWriteRepository<ProblemTemplate , Guid> problemTemplateWriteRepository ,
        IUnitOfWork unitOfWork ,
        ILogger<UpdateProblemTemplateCommandHandler> logger)
    {
        _currentUser = currentUser;
        _problemTemplateReadRepository = problemTemplateReadRepository;
        _problemTemplateWriteRepository = problemTemplateWriteRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ProblemTemplateDto> Handle(UpdateProblemTemplateCommand request , CancellationToken ct)
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        ProblemTemplateGuard.ValidateUpdateInput(request.CodeTemplateId , request.TemplateCode);

        var entity = await _problemTemplateReadRepository.FirstOrDefaultAsync(
            new ProblemTemplateTrackedByIdSpec(request.CodeTemplateId) ,
            ct);

        if ( entity is null )
            throw new KeyNotFoundException("Problem template not found.");

        entity.TemplateCode = request.TemplateCode.Trim();
        entity.InjectionPoint = ProblemTemplateGuard.NormalizeInjectionPoint(request.InjectionPoint);
        entity.SolutionSignature = string.IsNullOrWhiteSpace(request.SolutionSignature)
            ? null
            : request.SolutionSignature.Trim();

        // ép cứng theo problem mode hiện tại của problem
        entity.WrapperType = ProblemTemplateGuard.ResolveWrapperTypeFromProblemMode(entity.Problem.ProblemMode);

        if ( request.IsActive.HasValue )
            entity.IsActive = request.IsActive.Value;

        try
        {
            _problemTemplateWriteRepository.Update(entity);
            await _unitOfWork.SaveChangesAsync(ct);

            return entity.ToDto();
        }
        catch ( DbUpdateException ex )
        {
            _logger.LogError(
                ex ,
                "Database update failed while updating problem template. CodeTemplateId={CodeTemplateId}" ,
                request.CodeTemplateId);

            throw new InvalidOperationException("Failed to update problem template. Please verify input data and try again.");
        }
    }
}