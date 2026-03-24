using Application.Common.Interfaces;
using Application.UseCases.Problems.Dtos;
using Domain.Abstractions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.UseCases.Problems.Commands.UpdateProblem;

public sealed class UpdateProblemContentCommandHandler : IRequestHandler<UpdateProblemContentCommand , ProblemDetailDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProblemRepository _problemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProblemContentCommandHandler(
        ICurrentUserService currentUser ,
        IProblemRepository problemRepository ,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _problemRepository = problemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProblemDetailDto> Handle(UpdateProblemContentCommand request , CancellationToken ct)
    {
        if ( !_currentUser.IsAuthenticated || _currentUser.UserId is null )
            throw new UnauthorizedAccessException("User is not authenticated.");

        var currentUserId = _currentUser.UserId.Value;
        var isAdmin = _currentUser.IsInRole("Admin");

        var entity = await _problemRepository.GetProblemForManagementAsync(
            request.ProblemId ,
            currentUserId ,
            isAdmin ,
            ct);

        if ( entity is null )
            throw new KeyNotFoundException("Problem not found or access denied.");

        var title = request.Title?.Trim();
        var slug = request.Slug?.Trim().ToLowerInvariant();

        if ( string.IsNullOrWhiteSpace(title) )
            throw new ArgumentException("Title is required.");

        if ( string.IsNullOrWhiteSpace(slug) )
            throw new ArgumentException("Slug is required.");

        var slugExists = await _problemRepository.SlugExistsAsync(slug , entity.Id , ct);
        if ( slugExists )
            throw new InvalidOperationException($"Problem slug '{slug}' already exists.");

        entity.Title = title;
        entity.Slug = slug;
        entity.DescriptionMd = request.DescriptionMd;
        entity.TimeLimitMs = request.TimeLimitMs;
        entity.MemoryLimitKb = request.MemoryLimitKb;
        entity.TypeCode = request.TypeCode?.Trim();
        entity.ScoringCode = request.ScoringCode?.Trim();
        entity.VisibilityCode = request.VisibilityCode?.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = currentUserId;

        await _unitOfWork.SaveChangesAsync(ct);

        var detail = await _problemRepository.GetProblemDetailForManagementAsync(
            entity.Id ,
            currentUserId ,
            isAdmin ,
            ct);

        return detail ?? throw new KeyNotFoundException("Problem detail not found after update.");
    }
}