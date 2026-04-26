using Application.Common.Interfaces;
using Application.UseCases.Auth.Hasher;
using MediatR;

namespace Application.UseCases.Users.Commands.UpdateMyProfile;

public record UpdateMyProfileCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? Password) : IRequest;

public class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand>
{
    private readonly IUserManagementRepository _repo;
    private readonly IPasswordHasher _hasher;

    public UpdateMyProfileCommandHandler(IUserManagementRepository repo, IPasswordHasher hasher)
    {
        _repo = repo;
        _hasher = hasher;
    }

    public async Task Handle(UpdateMyProfileCommand req, CancellationToken ct)
    {
        var user = await _repo.FindUserAsync(req.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (!string.IsNullOrWhiteSpace(req.FirstName))
            user.FirstName = req.FirstName.Trim();
        if (!string.IsNullOrWhiteSpace(req.LastName))
            user.LastName = req.LastName.Trim();
        if (req.DisplayName != null)
            user.DisplayName = req.DisplayName.Trim();
        if (!string.IsNullOrWhiteSpace(req.Password))
            user.Password = _hasher.Hash(req.Password);

        user.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveAsync(ct);
    }
}
