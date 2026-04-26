using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Users.Commands.AssignRole;

public record AssignRoleCommand(Guid UserId, string RoleCode) : IRequest;

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand>
{
    private readonly IUserManagementRepository _repo;

    public AssignRoleCommandHandler(IUserManagementRepository repo) => _repo = repo;

    public async Task Handle(AssignRoleCommand req, CancellationToken ct)
    {
        var user = await _repo.FindUserAsync(req.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        var roleId = await _repo.GetRoleIdByCodeAsync(req.RoleCode.ToLowerInvariant(), ct)
            ?? throw new KeyNotFoundException("Role not found.");

        if (user.RoleId != roleId)
        {
            user.RoleId = roleId;
            await _repo.SaveAsync(ct);
        }
    }
}
