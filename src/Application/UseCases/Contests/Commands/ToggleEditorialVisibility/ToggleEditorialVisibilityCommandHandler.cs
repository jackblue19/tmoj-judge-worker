using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Contests.Commands;

public class ToggleEditorialVisibilityCommandHandler
    : IRequestHandler<ToggleEditorialVisibilityCommand, bool>
{
    private readonly IReadRepository<ContestProblem, Guid> _cpReadRepo;
    private readonly IWriteRepository<ContestProblem, Guid> _cpWriteRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ToggleEditorialVisibilityCommandHandler> _logger;

    public ToggleEditorialVisibilityCommandHandler(
        IReadRepository<ContestProblem, Guid> cpReadRepo,
        IWriteRepository<ContestProblem, Guid> cpWriteRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        ILogger<ToggleEditorialVisibilityCommandHandler> logger)
    {
        _cpReadRepo = cpReadRepo;
        _cpWriteRepo = cpWriteRepo;
        _currentUser = currentUser;
        _uow = uow;
        _logger = logger;
    }

    public async Task<bool> Handle(ToggleEditorialVisibilityCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("UNAUTHORIZED");

        if (!_currentUser.IsInRole("admin") && !_currentUser.IsInRole("manager"))
            throw new UnauthorizedAccessException("NO_PERMISSION");

        var userId = _currentUser.UserId!.Value;

        var cp = await _cpReadRepo.GetByIdAsync(request.ContestProblemId, ct)
            ?? throw new KeyNotFoundException("CONTEST_PROBLEM_NOT_FOUND");

        if (cp.ContestId != request.ContestId)
            throw new KeyNotFoundException("CONTEST_PROBLEM_NOT_FOUND");

        cp.ShowEditorial = request.ShowEditorial;

        _cpWriteRepo.Update(cp);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Editorial visibility toggled | CpId={CpId} | ShowEditorial={Show} | By={UserId}",
            cp.Id, request.ShowEditorial, userId);

        return true;
    }
}
