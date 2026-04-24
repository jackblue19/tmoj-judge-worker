using Application.Common.Interfaces;
using Application.UseCases.ProblemTemplates.Dtos;
using Application.UseCases.ProblemTemplates.Mappings;
using Application.UseCases.ProblemTemplates.Specifications;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.ProblemTemplates.Commands.DeleteProblemTemplate;

public sealed class DeleteProblemTemplateCommandHandler
    : IRequestHandler<DeleteProblemTemplateCommand , ProblemTemplateDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IReadRepository<ProblemTemplate , Guid> _problemTemplateReadRepository;
    private readonly IWriteRepository<ProblemTemplate , Guid> _problemTemplateWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteProblemTemplateCommandHandler> _logger;

    public DeleteProblemTemplateCommandHandler(
        ICurrentUserService currentUser ,
        IReadRepository<ProblemTemplate , Guid> problemTemplateReadRepository ,
        IWriteRepository<ProblemTemplate , Guid> problemTemplateWriteRepository ,
        IUnitOfWork unitOfWork ,
        ILogger<DeleteProblemTemplateCommandHandler> logger)
    {
        _currentUser = currentUser;
        _problemTemplateReadRepository = problemTemplateReadRepository;
        _problemTemplateWriteRepository = problemTemplateWriteRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ProblemTemplateDto> Handle(DeleteProblemTemplateCommand request , CancellationToken ct)
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        if ( request.CodeTemplateId == Guid.Empty )
            throw new ArgumentException("CodeTemplateId is required.");

        var entity = await _problemTemplateReadRepository.FirstOrDefaultAsync(
            new ProblemTemplateTrackedByIdSpec(request.CodeTemplateId) ,
            ct);

        if ( entity is null )
            throw new KeyNotFoundException("Problem template not found.");

        var currentUserId = _currentUser.UserId.Value;
        var isAdmin =
            _currentUser.IsInRole("admin") ||
            _currentUser.IsInRole("manager");

        if ( !isAdmin && entity.Problem.CreatedBy != currentUserId )
            throw new UnauthorizedAccessException("You do not have permission to delete this problem template.");

        if ( !entity.IsActive )
            return entity.ToDto();

        entity.IsActive = false;

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
                "Database update failed while deleting problem template. CodeTemplateId={CodeTemplateId}" ,
                request.CodeTemplateId);

            throw new InvalidOperationException("Failed to delete problem template. Please try again.");
        }
    }
}