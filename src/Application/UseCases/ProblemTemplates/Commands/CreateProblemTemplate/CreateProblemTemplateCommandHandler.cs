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

namespace Application.UseCases.ProblemTemplates.Commands.CreateProblemTemplate;

public sealed class CreateProblemTemplateCommandHandler
    : IRequestHandler<CreateProblemTemplateCommand , ProblemTemplateDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IReadRepository<Problem , Guid> _problemReadRepository;
    private readonly IReadRepository<Runtime , Guid> _runtimeReadRepository;
    private readonly IReadRepository<ProblemTemplate , Guid> _problemTemplateReadRepository;
    private readonly IWriteRepository<ProblemTemplate , Guid> _problemTemplateWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateProblemTemplateCommandHandler> _logger;

    public CreateProblemTemplateCommandHandler(
        ICurrentUserService currentUser ,
        IReadRepository<Problem , Guid> problemReadRepository ,
        IReadRepository<Runtime , Guid> runtimeReadRepository ,
        IReadRepository<ProblemTemplate , Guid> problemTemplateReadRepository ,
        IWriteRepository<ProblemTemplate , Guid> problemTemplateWriteRepository ,
        IUnitOfWork unitOfWork ,
        ILogger<CreateProblemTemplateCommandHandler> logger)
    {
        _currentUser = currentUser;
        _problemReadRepository = problemReadRepository;
        _runtimeReadRepository = runtimeReadRepository;
        _problemTemplateReadRepository = problemTemplateReadRepository;
        _problemTemplateWriteRepository = problemTemplateWriteRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ProblemTemplateDto> Handle(CreateProblemTemplateCommand request , CancellationToken ct)
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        ProblemTemplateGuard.ValidateCreateInput(
            request.ProblemId ,
            request.RuntimeId ,
            request.TemplateCode ,
            request.Version);

        var problem = await _problemReadRepository.FirstOrDefaultAsync(
            new ActiveProblemByIdSpec(request.ProblemId) , ct);

        if ( problem is null )
            throw new KeyNotFoundException("Problem not found.");

        var runtimeExists = await _runtimeReadRepository.AnyAsync(
            new ActiveRuntimeByIdSpec(request.RuntimeId) , ct);

        if ( !runtimeExists )
            throw new KeyNotFoundException("Runtime not found.");

        var normalizedVersion = ProblemTemplateGuard.NormalizeVersion(request.Version);

        var duplicateExists = await _problemTemplateReadRepository.AnyAsync(
            new ProblemTemplateByProblemRuntimeVersionSpec(
                request.ProblemId ,
                request.RuntimeId ,
                normalizedVersion) ,
            ct);

        if ( duplicateExists )
            throw new InvalidOperationException("A template with the same problem, runtime, and version already exists.");

        var now = DateTime.UtcNow;

        var entity = new ProblemTemplate
        {
            CodeTemplateId = Guid.NewGuid() ,
            ProblemId = request.ProblemId ,
            RuntimeId = request.RuntimeId ,
            TemplateCode = request.TemplateCode.Trim() ,
            InjectionPoint = ProblemTemplateGuard.NormalizeInjectionPoint(request.InjectionPoint) ,
            SolutionSignature = string.IsNullOrWhiteSpace(request.SolutionSignature)
                ? null
                : request.SolutionSignature.Trim() ,

            // ép cứng theo problem mode
            WrapperType = ProblemTemplateGuard.ResolveWrapperTypeFromProblemMode(problem.ProblemMode) ,

            Version = normalizedVersion ,
            IsActive = true ,
            CreatedAt = now ,
            CreatedBy = _currentUser.UserId.Value
        };

        try
        {
            await _problemTemplateWriteRepository.AddAsync(entity , ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return entity.ToDto();
        }
        catch ( DbUpdateException ex )
        {
            _logger.LogError(ex ,
                "Database update failed while creating problem template. ProblemId={ProblemId}, RuntimeId={RuntimeId}, Version={Version}" ,
                request.ProblemId , request.RuntimeId , normalizedVersion);

            throw new InvalidOperationException("Failed to save problem template. Please verify input data and try again.");
        }
    }
}