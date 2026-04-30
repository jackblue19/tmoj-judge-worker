using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Gamification.Commands.CreateBadge;

public class CreateBadgeHandler : IRequestHandler<CreateBadgeCommand, Guid>
{
    private readonly IGamificationRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly ICloudinaryService _cloudinary;

    public CreateBadgeHandler(
        IGamificationRepository repo,
        ICurrentUserService currentUser,
        ICloudinaryService cloudinary)
    {
        _repo = repo;
        _currentUser = currentUser;
        _cloudinary = cloudinary;
    }

    public async Task<Guid> Handle(CreateBadgeCommand request, CancellationToken cancellationToken)
    {
        // validate category
        var validCategories = new[] { "contest", "course", "org", "streak", "problem" };

        if (!validCategories.Contains(request.BadgeCategory))
            throw new Exception("Invalid badge category");

        // check duplicate code
        var exists = await _repo.ExistsBadgeCodeAsync(request.BadgeCode);

        if (exists)
            throw new Exception("Badge code already exists");

        string? iconUrl = request.IconUrl;
        
        if (request.IconFile != null && request.IconFile.Length > 0)
        {
            var uploadResult = await _cloudinary.UploadImageAsync(request.IconFile);
            if (uploadResult != null && !string.IsNullOrEmpty(uploadResult.Url))
            {
                iconUrl = uploadResult.Url;
            }
        }

        var badge = new Badge
        {
            BadgeId = Guid.NewGuid(),
            Name = request.Name,
            IconUrl = iconUrl,
            Description = request.Description,
            BadgeCode = request.BadgeCode,
            BadgeCategory = request.BadgeCategory,
            BadgeLevel = request.BadgeLevel,
            IsRepeatable = request.IsRepeatable,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };

        return await _repo.CreateBadgeAsync(badge);
    }
}