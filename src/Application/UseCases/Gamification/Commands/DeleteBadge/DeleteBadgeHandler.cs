using Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Gamification.Commands.DeleteBadge
{
    public class DeleteBadgeHandler : IRequestHandler<DeleteBadgeCommand, bool>
    {
        private readonly IGamificationRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<DeleteBadgeHandler> _logger;

        public DeleteBadgeHandler(
            IGamificationRepository repo,
            ICurrentUserService currentUser,
            ILogger<DeleteBadgeHandler> logger)
        {
            _repo = repo;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<bool> Handle(DeleteBadgeCommand request, CancellationToken ct)
        {
            _logger.LogInformation("Deleting badge {BadgeId}", request.BadgeId);

            // 🔐 ROLE CHECK
            if (!_currentUser.IsInRole("admin"))
                throw new UnauthorizedAccessException("Only admin can delete badge");

            // 🔥 VALIDATION
            if (request.BadgeId == Guid.Empty)
                throw new ArgumentException("BadgeId is required");

            var badge = await _repo.GetBadgeByIdAsync(request.BadgeId);

            if (badge == null)
            {
                _logger.LogWarning("Badge not found");
                return false;
            }

            // 🚫 BUSINESS RULE
            var isUsed = await _repo.IsBadgeUsedAsync(request.BadgeId);
            if (isUsed)
                throw new Exception("Cannot delete badge already assigned to users");

            await _repo.DeleteBadgeAsync(badge);
            await _repo.SaveChangesAsync();

            _logger.LogInformation("Badge deleted successfully");

            return true;
        }
    }
}