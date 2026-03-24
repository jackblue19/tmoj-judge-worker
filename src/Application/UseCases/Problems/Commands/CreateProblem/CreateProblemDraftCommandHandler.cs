using Application.Common.Interfaces;
using Application.UseCases.Problems.Constants;
using Application.UseCases.Problems.Dtos;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Problems.Commands.CreateProblem;

public sealed class CreateProblemDraftCommandHandler : IRequestHandler<CreateProblemDraftCommand, ProblemSummaryDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProblemRepository _problemRepository;
    private readonly IWriteRepository<Problem, Guid> _problemWriteRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProblemDraftCommandHandler(
        ICurrentUserService currentUser,
        IProblemRepository problemRepository,
        IWriteRepository<Problem, Guid> problemWriteRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _problemRepository = problemRepository;
        _problemWriteRepository = problemWriteRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProblemSummaryDto> Handle(CreateProblemDraftCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var title = request.Title?.Trim();
        var slug = request.Slug?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug is required.");

        var slugExists = await _problemRepository.SlugExistsAsync(slug, null, ct);
        if (slugExists)
            throw new InvalidOperationException($"Problem slug '{slug}' already exists.");

        var now = DateTime.UtcNow;

        var entity = new Problem
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            StatusCode = ProblemStatusCodes.Draft,
            TypeCode = request.TypeCode?.Trim(),
            ScoringCode = request.ScoringCode?.Trim(),
            VisibilityCode = request.VisibilityCode?.Trim(),
            DescriptionMd = request.DescriptionMd,
            TimeLimitMs = request.TimeLimitMs,
            MemoryLimitKb = request.MemoryLimitKb,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = _currentUser.UserId.Value,
            UpdatedAt = now,
            UpdatedBy = _currentUser.UserId.Value
        };

        await _problemWriteRepository.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new ProblemSummaryDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Slug = entity.Slug,
            StatusCode = entity.StatusCode,
            Difficulty = entity.Difficulty,
            TimeLimitMs = entity.TimeLimitMs,
            MemoryLimitKb = entity.MemoryLimitKb,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}