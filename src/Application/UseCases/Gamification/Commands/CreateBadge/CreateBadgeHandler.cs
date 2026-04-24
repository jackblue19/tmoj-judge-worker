using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Gamification.Commands.CreateBadge;

public class CreateBadgeHandler : IRequestHandler<CreateBadgeCommand, Guid>
{
    private readonly IGamificationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public CreateBadgeHandler(
        IGamificationRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateBadgeCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        // validate category
        var validCategories = new[] { "contest", "course", "org", "streak", "problem" };

        if (!validCategories.Contains(dto.BadgeCategory))
            throw new Exception("Invalid badge category");

        // check duplicate code
        var exists = await _repo.ExistsBadgeCodeAsync(dto.BadgeCode);

        if (exists)
            throw new Exception("Badge code already exists");

        var badge = new Badge
        {
            BadgeId = Guid.NewGuid(),
            Name = dto.Name,
            IconUrl = dto.IconUrl,
            Description = dto.Description,
            BadgeCode = dto.BadgeCode,
            BadgeCategory = dto.BadgeCategory,
            BadgeLevel = dto.BadgeLevel,
            IsRepeatable = dto.IsRepeatable,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.UserId
        };

        return await _repo.CreateBadgeAsync(badge);
    }
}