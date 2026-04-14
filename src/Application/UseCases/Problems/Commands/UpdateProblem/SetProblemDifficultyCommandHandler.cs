using Application.Common.Interfaces;
using Application.UseCases.Problems.Constants;
using Application.UseCases.Problems.Dtos;
using Domain.Abstractions;
using MediatR;

namespace Application.UseCases.Problems.Commands.UpdateProblem;


public sealed class SetProblemDifficultyCommandHandler : IRequestHandler<SetProblemDifficultyCommand, ProblemDetailDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProblemRepository _problemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetProblemDifficultyCommandHandler(
        ICurrentUserService currentUser,
        IProblemRepository problemRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _problemRepository = problemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProblemDetailDto> Handle(SetProblemDifficultyCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var difficulty = request.Difficulty?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(difficulty))
            throw new ArgumentException("Difficulty is required.");

        if (!ProblemDifficultyCodes.Allowed.Contains(difficulty))
            throw new ArgumentException("Difficulty must be one of: easy, medium, hard.");

        var currentUserId = _currentUser.UserId.Value;
        var isAdmin = _currentUser.IsInRole("Admin") || _currentUser.IsInRole("admin");

        var entity = await _problemRepository.GetProblemForManagementAsync(
            request.ProblemId,
            currentUserId,
            isAdmin,
            ct);

        if (entity is null)
            throw new KeyNotFoundException("Problem not found or access denied.");

        entity.Difficulty = difficulty;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = currentUserId;

        await _unitOfWork.SaveChangesAsync(ct);

        var detail = await _problemRepository.GetProblemDetailForManagementAsync(
            entity.Id,
            currentUserId,
            isAdmin,
            ct);

        return detail ?? throw new KeyNotFoundException("Problem detail not found after update.");
    }
}