using Application.Common.Events;
using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Gamification.Events;

public class ContestFinishedEventHandler
    : INotificationHandler<ContestFinishedEvent>
{
    private readonly IContestRepository _contestRepository;
    private readonly IGamificationRepository _repo;

    public ContestFinishedEventHandler(
        IContestRepository contestRepository,
        IGamificationRepository repo)
    {
        _contestRepository = contestRepository;
        _repo = repo;
    }

    public async Task Handle(ContestFinishedEvent notification, CancellationToken ct)
    {
        var contestId = notification.ContestId;

        var scoreboard = await _contestRepository.GetScoreboardAsync(contestId);

        foreach (var item in scoreboard)
        {
            // TOP 1 badge
            if (item.Rank == 1)
            {
                await _repo.AddUserBadgeAsync(new Domain.Entities.UserBadge
                {
                    UserId = item.TeamId,
                    BadgeId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    AwardedAt = DateTime.UtcNow
                });
            }

            // TOP 3 badge
            if (item.Rank <= 3)
            {
                await _repo.AddUserBadgeAsync(new Domain.Entities.UserBadge
                {
                    UserId = item.TeamId,
                    BadgeId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    AwardedAt = DateTime.UtcNow
                });
            }
        }

        await _repo.SaveChangesAsync();
    }
}