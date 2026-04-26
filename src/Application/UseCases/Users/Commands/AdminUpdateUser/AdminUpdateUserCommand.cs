using Application.Common.Interfaces;
using Application.UseCases.Auth.Hasher;
using MediatR;

namespace Application.UseCases.Users.Commands.AdminUpdateUser;

public record AdminUpdateUserCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? Username,
    string? DisplayName,
    string? Password,
    string? RoleCode,
    bool? Status) : IRequest;

public class AdminUpdateUserCommandHandler : IRequestHandler<AdminUpdateUserCommand>
{
    private readonly IUserManagementRepository _repo;
    private readonly IPasswordHasher _hasher;

    public AdminUpdateUserCommandHandler(IUserManagementRepository repo, IPasswordHasher hasher)
    {
        _repo = repo;
        _hasher = hasher;
    }

    public async Task Handle(AdminUpdateUserCommand req, CancellationToken ct)
    {
        var user = await _repo.FindUserAsync(req.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (!string.IsNullOrWhiteSpace(req.FirstName))
            user.FirstName = req.FirstName.Trim();
        if (!string.IsNullOrWhiteSpace(req.LastName))
            user.LastName = req.LastName.Trim();
        if (!string.IsNullOrWhiteSpace(req.Username))
        {
            var norm = req.Username.Trim().ToLowerInvariant();
            if (await _repo.UsernameExistsAsync(norm, req.UserId, ct))
                throw new InvalidOperationException("Username is already taken.");
            user.Username = norm;
        }
        if (req.DisplayName != null)
            user.DisplayName = req.DisplayName.Trim();
        if (!string.IsNullOrWhiteSpace(req.Password))
            user.Password = _hasher.Hash(req.Password);
        if (!string.IsNullOrWhiteSpace(req.RoleCode))
        {
            var roleId = await _repo.GetRoleIdByCodeAsync(req.RoleCode.Trim().ToLowerInvariant(), ct)
                ?? throw new KeyNotFoundException("Role not found.");
            user.RoleId = roleId;
        }
        if (req.Status.HasValue)
            user.Status = req.Status.Value;

        user.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveAsync(ct);
    }
}
