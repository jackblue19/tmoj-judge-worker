using Application.Abstractions.Outbound.Services;
using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Users.Commands.DeleteAvatar;

public record DeleteAvatarCommand(Guid UserId) : IRequest;

public class DeleteAvatarCommandHandler : IRequestHandler<DeleteAvatarCommand>
{
    private readonly IUserManagementRepository _repo;
    private readonly ICloudinaryService _cloudinary;

    public DeleteAvatarCommandHandler(IUserManagementRepository repo, ICloudinaryService cloudinary)
    {
        _repo = repo;
        _cloudinary = cloudinary;
    }

    public async Task Handle(DeleteAvatarCommand req, CancellationToken ct)
    {
        var user = await _repo.FindUserAsync(req.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (string.IsNullOrEmpty(user.AvatarUrl))
            throw new InvalidOperationException("No avatar to delete.");

        if (TryGetAvatarId(user.AvatarUrl, out var avatarId))
            await _cloudinary.DeleteAvatarAsync(avatarId, ct);

        user.AvatarUrl = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveAsync(ct);
    }

    private static bool TryGetAvatarId(string? url, out Guid avatarId)
    {
        avatarId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (Guid.TryParse(url, out avatarId)) return true;
        var fileName = url.Split('/').LastOrDefault();
        if (fileName != null)
            return Guid.TryParse(Path.GetFileNameWithoutExtension(fileName), out avatarId);
        return false;
    }
}
