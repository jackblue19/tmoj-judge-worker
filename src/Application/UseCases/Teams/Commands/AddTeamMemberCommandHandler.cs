using Application.Common.Interfaces;
using Application.UseCases.Teams.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Teams.Commands;

public class AddTeamMemberCommandHandler
    : IRequestHandler<AddTeamMemberCommand, Guid>
{
    private readonly IReadRepository<Team, Guid> _teamRepo;
    private readonly IReadRepository<TeamMember, Guid> _memberReadRepo;
    private readonly IWriteRepository<TeamMember, Guid> _memberWriteRepo;
    private readonly IWriteRepository<Team, Guid> _teamWriteRepo;
    private readonly ITeamRepository _teamCustomRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public AddTeamMemberCommandHandler(
        IReadRepository<Team, Guid> teamRepo,
        IReadRepository<TeamMember, Guid> memberReadRepo,
        IWriteRepository<TeamMember, Guid> memberWriteRepo,
        IWriteRepository<Team, Guid> teamWriteRepo,
        ITeamRepository teamCustomRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _teamRepo = teamRepo;
        _memberReadRepo = memberReadRepo;
        _memberWriteRepo = memberWriteRepo;
        _teamWriteRepo = teamWriteRepo;
        _teamCustomRepo = teamCustomRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<Guid> Handle(AddTeamMemberCommand request, CancellationToken ct)
    {
        var currentUserId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        // ======================
        // 1. CHECK TEAM
        // ======================
        var team = await _teamRepo.GetByIdAsync(request.TeamId, ct);
        if (team == null)
            throw new Exception("Team not found");

        // ======================
        // 2. CHECK LEADER
        // ======================
        var isLeader = await _teamCustomRepo
            .IsUserLeaderAsync(currentUserId, request.TeamId);

        if (!isLeader)
            throw new Exception("Only leader can add members");

        // ======================
        // 3. CHECK USER ALREADY IN TEAM
        // ======================
        var isInAnyTeam = await _teamCustomRepo
            .IsUserInTeamAsync(request.UserId, request.TeamId);

        if (isInAnyTeam)
            throw new Exception("User already in this team");

        // ======================
        // 4. GET MEMBERS
        // ======================
        var members = await _memberReadRepo.ListAsync(
            new TeamMemberByTeamSpec(request.TeamId), ct);

        if (members.Count >= 5)
            throw new Exception("Team max size is 5");

        // ======================
        // 5. ADD MEMBER
        // ======================
        var entity = new TeamMember
        {
            TeamId = request.TeamId,
            UserId = request.UserId,
            JoinedAt = DateTime.UtcNow
        };

        await _memberWriteRepo.AddAsync(entity, ct);

        // ======================
        // 6. UPDATE TEAM SIZE
        // ======================
        team.TeamSize = members.Count + 1;
        team.UpdatedAt = DateTime.UtcNow;

        _teamWriteRepo.Update(team);

        await _uow.SaveChangesAsync(ct);

        return request.UserId;
    }
}