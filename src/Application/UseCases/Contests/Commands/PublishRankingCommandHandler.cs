using Application.Common.Interfaces;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Contests.Commands;

public class PublishRankingCommandHandler
    : IRequestHandler<PublishRankingCommand, bool>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IWriteRepository<Contest, Guid> _contestWriteRepo;
    private readonly IReadRepository<ContestTeam, Guid> _contestTeamRepo;
    private readonly IWriteRepository<ContestHistory, Guid> _historyWriteRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PublishRankingCommandHandler> _logger;

    public PublishRankingCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IWriteRepository<Contest, Guid> contestWriteRepo,
        IReadRepository<ContestTeam, Guid> contestTeamRepo,
        IWriteRepository<ContestHistory, Guid> historyWriteRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        ILogger<PublishRankingCommandHandler> logger)
    {
        _contestRepo = contestRepo;
        _contestWriteRepo = contestWriteRepo;
        _contestTeamRepo = contestTeamRepo;
        _historyWriteRepo = historyWriteRepo;
        _currentUser = currentUser;
        _uow = uow;
        _logger = logger;
    }

    public async Task<bool> Handle(PublishRankingCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("UNAUTHORIZED");

        if (!_currentUser.IsInRole("admin") && !_currentUser.IsInRole("manager"))
            throw new UnauthorizedAccessException("NO_PERMISSION");

        var userId = _currentUser.UserId!.Value;

        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);

        if (contest == null)
            throw new Exception("CONTEST_NOT_FOUND");

        var now = DateTime.UtcNow;

        if (contest.EndAt > now)
            throw new Exception("CONTEST_NOT_ENDED");

        // Unfreeze if frozen
        if (contest.FreezeAt.HasValue)
        {
            contest.FreezeAt = null;
            contest.UpdatedAt = now;
            contest.UpdatedBy = userId;
            _contestWriteRepo.Update(contest);
        }

        // Load all teams with members
        var contestTeams = await _contestTeamRepo.ListAsync(
            new ContestTeamsWithMembersSpec(request.ContestId), ct);

        var histories = new List<ContestHistory>();

        foreach (var ct2 in contestTeams)
        {
            var history = new ContestHistory
            {
                HistoryId = Guid.NewGuid(),
                ContestId = contest.Id,
                Score = ct2.Score.HasValue ? (int)ct2.Score.Value : null,
                Ranking = ct2.Rank,
                ParticipatedAt = now,
                Users = ct2.Team.TeamMembers
                    .Select(tm => tm.User)
                    .ToList()
            };

            histories.Add(history);
        }

        if (histories.Count > 0)
            await _historyWriteRepo.AddRangeAsync(histories, ct);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Ranking published | ContestId={ContestId} | Teams={Count} | By={UserId}",
            contest.Id, histories.Count, userId);

        return true;
    }
}
