using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Teams.Commands;

public class CreateTeamCommandHandler : IRequestHandler<CreateTeamCommand, CreateTeamResponse>
{
    private readonly IWriteRepository<Team, Guid> _teamRepo;
    private readonly IWriteRepository<TeamMember, Guid> _memberRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public CreateTeamCommandHandler(
        IWriteRepository<Team, Guid> teamRepo,
        IWriteRepository<TeamMember, Guid> memberRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _teamRepo = teamRepo;
        _memberRepo = memberRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<CreateTeamResponse> Handle(CreateTeamCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        if (string.IsNullOrWhiteSpace(request.TeamName))
            throw new Exception("Team name is required");

        var teamId = Guid.NewGuid();
        var inviteCode = Guid.NewGuid().ToString("N")[..8];

        var team = new Team
        {
            Id = teamId,
            LeaderId = userId,
            TeamName = request.TeamName,
            AvatarUrl = request.AvatarUrl,
            InviteCode = inviteCode,
            IsPersonal = false,
            TeamSize = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _teamRepo.AddAsync(team, ct);

        await _memberRepo.AddAsync(new TeamMember
        {
            TeamId = teamId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        }, ct);

        await _uow.SaveChangesAsync(ct);

        return new CreateTeamResponse
        {
            TeamId = teamId,
            InviteCode = inviteCode
        };
    }
}