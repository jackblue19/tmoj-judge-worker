using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Teams.Commands;

public class JoinTeamByCodeCommandHandler
    : IRequestHandler<JoinTeamByCodeCommand>
{
    private readonly ITeamRepository _repo;
    private readonly IWriteRepository<TeamMember, Guid> _writeRepo;
    private readonly IWriteRepository<Team, Guid> _teamWriteRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public JoinTeamByCodeCommandHandler(
        ITeamRepository repo,
        IWriteRepository<TeamMember, Guid> writeRepo,
        IWriteRepository<Team, Guid> teamWriteRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _repo = repo;
        _writeRepo = writeRepo;
        _teamWriteRepo = teamWriteRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task Handle(JoinTeamByCodeCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        // =========================
        // 1. CHECK TEAM EXIST
        // =========================
        var team = await _repo.GetByInviteCodeAsync(request.Code);
        if (team == null)
            throw new Exception("Invalid code");

        // =========================
        // 2. CHECK USER ALREADY IN TEAM
        // =========================
        var isAlreadyInTeam = await _repo
            .IsUserInTeamAsync(userId, team.Id);

        if (isAlreadyInTeam)
            throw new Exception("User already in this team");

        // =========================
        // 3. CHECK USER HAS OTHER TEAM (NON PERSONAL)
        // =========================
        var teams = await _repo.GetTeamsByUserAsync(userId);

        if (teams.Any(x => !x.IsPersonal))
            throw new Exception("User already in another team");

        // =========================
        // 4. CHECK TEAM SIZE
        // =========================
        var count = await _repo.GetTeamMemberCountAsync(team.Id);

        if (count >= 5)
            throw new Exception("Team full");

        // =========================
        // 5. ADD MEMBER
        // =========================
        var entity = new TeamMember
        {
            TeamId = team.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };

        await _writeRepo.AddAsync(entity, ct);

        // =========================
        // 6. UPDATE TEAM SIZE
        // =========================
        team.TeamSize = count + 1;
        team.UpdatedAt = DateTime.UtcNow;

        _teamWriteRepo.Update(team);

        await _uow.SaveChangesAsync(ct);
    }
}