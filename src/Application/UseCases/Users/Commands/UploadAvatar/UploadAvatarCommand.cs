using Application.Abstractions.Outbound.Services;
using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Users.Commands.UploadAvatar;

public record UploadAvatarCommand(Guid UserId, Stream FileStream, string Extension) : IRequest<string>;

public class UploadAvatarCommandHandler : IRequestHandler<UploadAvatarCommand, string>
{
    private readonly IUserManagementRepository _repo;
    private readonly ICloudinaryService _cloudinary;

    public UploadAvatarCommandHandler(IUserManagementRepository repo, ICloudinaryService cloudinary)
    {
        _repo = repo;
        _cloudinary = cloudinary;
    }

    public async Task<string> Handle(UploadAvatarCommand req, CancellationToken ct)
    {
        var user = await _repo.FindUserAsync(req.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (TryGetAvatarId(user.AvatarUrl, out var existingId))
        {
            await _cloudinary.ReplaceAvatarAsync(existingId, req.FileStream, req.Extension, ct);
            user.AvatarUrl = _cloudinary.GetAvatarUrl(existingId);
        }
        else
        {
            var avatarId = await _cloudinary.UploadAvatarAsync(req.FileStream, req.Extension, ct);
            user.AvatarUrl = _cloudinary.GetAvatarUrl(avatarId);
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveAsync(ct);
        return user.AvatarUrl;
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
