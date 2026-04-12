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
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public JoinTeamByCodeCommandHandler(
        ITeamRepository repo,
        IWriteRepository<TeamMember, Guid> writeRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _repo = repo;
        _writeRepo = writeRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task Handle(JoinTeamByCodeCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        // ❌ đã có team chưa
        var teams = await _repo.GetTeamsByUserAsync(userId);
        if (teams.Any(x => !x.IsPersonal))
            throw new Exception("User already in a team");

        var team = await _repo.GetByInviteCodeAsync(request.Code);
        if (team == null)
            throw new Exception("Invalid code");

        var count = await _repo.GetTeamMemberCountAsync(team.Id);
        if (count >= 5)
            throw new Exception("Team full");

        await _writeRepo.AddAsync(new TeamMember
        {
            TeamId = team.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        }, ct);

        await _uow.SaveChangesAsync(ct);
    }
}