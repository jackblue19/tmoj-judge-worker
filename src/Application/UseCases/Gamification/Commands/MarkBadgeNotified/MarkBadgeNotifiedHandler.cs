using Application.Common.Interfaces;
using MediatR;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Gamification.Commands.MarkBadgeNotified;

public class MarkBadgeNotifiedHandler : IRequestHandler<MarkBadgeNotifiedCommand, bool>
{
    private readonly IGamificationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public MarkBadgeNotifiedHandler(IGamificationRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(MarkBadgeNotifiedCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (userId == null || userId == Guid.Empty)
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để thực hiện hành động này.");

        var userBadges = await _repo.GetUserBadgesAsync(userId.Value);
        var badge = userBadges.FirstOrDefault(x => x.BadgeId == request.BadgeId);

        if (badge == null)
            return false; // User chưa có huy hiệu này

        // Cập nhật MetaJson để thêm isNotified: true
        if (string.IsNullOrEmpty(badge.MetaJson))
        {
            badge.MetaJson = "{\"isNotified\":true}";
        }
        else
        {
            try
            {
                var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(badge.MetaJson);
                if (jsonNode is System.Text.Json.Nodes.JsonObject jsonObject)
                {
                    jsonObject["isNotified"] = true;
                    badge.MetaJson = jsonObject.ToJsonString();
                }
            }
            catch
            {
                // Fallback nếu json lỗi
                badge.MetaJson = "{\"isNotified\":true}";
            }
        }

        await _repo.UpdateUserBadgeAsync(badge);
        await _repo.SaveChangesAsync();

        return true;
    }
}
