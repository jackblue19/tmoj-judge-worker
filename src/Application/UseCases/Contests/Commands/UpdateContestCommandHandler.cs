using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Contests.Commands;

public class UpdateContestCommandHandler
    : IRequestHandler<UpdateContestCommand, bool>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IWriteRepository<Contest, Guid> _writeRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public UpdateContestCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IWriteRepository<Contest, Guid> writeRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _contestRepo = contestRepo;
        _writeRepo = writeRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateContestCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("UNAUTHORIZED");

        var userId = _currentUser.UserId!.Value;

        Console.WriteLine("=== UPDATE CONTEST START ===");
        Console.WriteLine($"UserId: {userId}");
        Console.WriteLine($"ContestId: {request.ContestId}");

        // =========================
        // 1. GET CONTEST
        // =========================
        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);

        if (contest == null)
            throw new Exception("CONTEST_NOT_FOUND");

        // =========================
        // 2. CHECK PERMISSION
        // =========================
        var isOwner = contest.CreatedBy == userId;
        var isAdmin =
            _currentUser.IsInRole("admin") ||
            _currentUser.IsInRole("manager");

        if (!isOwner && !isAdmin)
            throw new UnauthorizedAccessException("NO_PERMISSION");

        Console.WriteLine($"isOwner={isOwner}, isAdmin={isAdmin}");

        // =========================
        // 3. CHECK TIME
        // =========================
        if (contest.StartAt <= DateTime.UtcNow)
            throw new Exception("CANNOT_UPDATE_AFTER_START");

        // =========================
        // 4. VALIDATION
        // =========================
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new Exception("TITLE_REQUIRED");

        if (request.StartAt >= request.EndAt)
            throw new Exception("INVALID_TIME_RANGE");

        // =========================
        // 5. UPDATE
        // =========================
        contest.Title = request.Title;
        contest.DescriptionMd = request.Description;

        contest.StartAt = DateTime.SpecifyKind(request.StartAt, DateTimeKind.Utc);
        contest.EndAt = DateTime.SpecifyKind(request.EndAt, DateTimeKind.Utc);

        contest.VisibilityCode = request.VisibilityCode;
        contest.ContestType = request.ContestType;
        contest.AllowTeams = request.AllowTeams;

        contest.UpdatedAt = DateTime.UtcNow;
        contest.UpdatedBy = userId;

        _writeRepo.Update(contest);
        await _uow.SaveChangesAsync(ct);

        Console.WriteLine("=== UPDATE CONTEST SUCCESS ===");

        return true;
    }
}