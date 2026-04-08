using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Contests.Commands;

public class JoinContestCommandHandler
    : IRequestHandler<JoinContestCommand, Guid>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IReadRepository<Team, Guid> _teamRepo;
    private readonly IWriteRepository<ContestTeam, Guid> _contestTeamRepo;
    private readonly IWriteRepository<Team, Guid> _teamWriteRepo;
    private readonly IContestRepository _contestCustomRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public JoinContestCommandHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IReadRepository<Team, Guid> teamRepo,
        IWriteRepository<ContestTeam, Guid> contestTeamRepo,
        IWriteRepository<Team, Guid> teamWriteRepo,
        IContestRepository contestCustomRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _contestRepo = contestRepo;
        _teamRepo = teamRepo;
        _contestTeamRepo = contestTeamRepo;
        _teamWriteRepo = teamWriteRepo;
        _contestCustomRepo = contestCustomRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<Guid> Handle(JoinContestCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;

        if (!userId.HasValue)
            throw new UnauthorizedAccessException("User not authenticated");

        // ===============================
        // 1. GET CONTEST
        // ===============================
        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);

        if (contest == null)
            throw new Exception("Contest not found");

        var now = DateTime.UtcNow;

        if (contest.EndAt < now)
            throw new Exception("Contest has ended");

        // ===============================
        // 2. DETERMINE TEAM
        // ===============================
        Guid teamId;

        if (contest.AllowTeams)
        {
            if (!request.TeamId.HasValue)
                throw new Exception("TeamId is required");

            var team = await _teamRepo.GetByIdAsync(request.TeamId.Value, ct);

            if (team == null)
                throw new Exception("Team not found");

            // 🔥 CHECK USER BELONG TO TEAM (FIX QUAN TRỌNG)
            var isMember = await _contestCustomRepo
                .IsUserInTeamAsync(userId.Value, team.Id);

            if (!isMember)
                throw new Exception("You are not a member of this team");

            teamId = team.Id;
        }
        else
        {
            // create personal team
            var team = new Team
            {
                Id = Guid.NewGuid(),
                LeaderId = userId.Value,
                TeamName = $"User-{userId}",
                TeamSize = 1,
                IsPersonal = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _teamWriteRepo.AddAsync(team, ct);

            teamId = team.Id;
        }

        // ===============================
        // 3. CHECK ALREADY JOINED
        // ===============================
        var alreadyJoined = await _contestCustomRepo
            .IsTeamJoinedAsync(request.ContestId, teamId);

        if (alreadyJoined)
            throw new Exception("Already joined this contest");

        // ===============================
        // 4. INSERT
        // ===============================
        var entry = new ContestTeam
        {
            Id = Guid.NewGuid(),
            ContestId = request.ContestId,
            TeamId = teamId,
            JoinAt = DateTime.UtcNow,
            Score = 0,
            SolvedProblem = 0,
            SubmissionsCount = 0,
            Penalty = 0
        };

        await _contestTeamRepo.AddAsync(entry, ct);
        await _uow.SaveChangesAsync(ct);

        return entry.Id;
    }
}