//using Application.Common.Interfaces;
//using MediatR;
//using Microsoft.Extensions.Logging;

//namespace Application.UseCases.Gamification.Commands.UpdateBadge
//{
//    public class UpdateBadgeHandler : IRequestHandler<UpdateBadgeCommand, bool>
//    {
//        private readonly IGamificationRepository _repo;
//        private readonly ICurrentUserService _currentUser;
//        private readonly ILogger<UpdateBadgeHandler> _logger;
//        private readonly ICloudinaryService _cloudinary;

//        private static readonly string[] AllowedCategories =
//            { "contest", "course", "org", "streak", "problem" };

//        public UpdateBadgeHandler(
//            IGamificationRepository repo,
//            ICurrentUserService currentUser,
//            ILogger<UpdateBadgeHandler> logger,
//            ICloudinaryService cloudinary)
//        {
//            _repo = repo;
//            _currentUser = currentUser;
//            _logger = logger;
//            _cloudinary = cloudinary;
//        }

//        public async Task<bool> Handle(UpdateBadgeCommand request, CancellationToken ct)
//        {
//            _logger.LogInformation("Updating badge {BadgeId}", request.BadgeId);

//            // 🔐 ROLE CHECK
//            if (!_currentUser.IsInRole("admin"))
//                throw new UnauthorizedAccessException("Only admin can update badge");

//            // 🔥 VALIDATION
//            if (request.BadgeId == Guid.Empty)
//                throw new ArgumentException("BadgeId is required");

//            if (string.IsNullOrWhiteSpace(request.Name))
//                throw new ArgumentException("Name is required");

//            if (!AllowedCategories.Contains(request.BadgeCategory))
//                throw new ArgumentException("Invalid badge category");

//            if (request.BadgeLevel < 1 || request.BadgeLevel > 10)
//                throw new ArgumentException("BadgeLevel must be between 1 and 10");

//            // 🧠 GET DATA
//            var badge = await _repo.GetBadgeByIdAsync(request.BadgeId);

//            if (badge == null)
//            {
//                _logger.LogWarning("Badge not found");
//                return false;
//            }

//            // ✏️ UPDATE
//            badge.Name = request.Name;
//            badge.Description = request.Description;
            
//            if (request.IconFile != null && request.IconFile.Length > 0)
//            {
//                var uploadResult = await _cloudinary.UploadImageAsync(request.IconFile);
//                if (uploadResult != null && !string.IsNullOrEmpty(uploadResult.Url))
//                {
//                    badge.IconUrl = uploadResult.Url;
//                }
//            }
//            else if (!string.IsNullOrEmpty(request.IconUrl))
//            {
//                badge.IconUrl = request.IconUrl;
//            }

//            badge.BadgeCategory = request.BadgeCategory;
//            badge.BadgeLevel = request.BadgeLevel;
//            badge.UpdatedAt = DateTime.UtcNow;

//            await _repo.UpdateBadgeAsync(badge);
//            await _repo.SaveChangesAsync();

//            _logger.LogInformation("Badge updated successfully");

//            return true;
//        }
//    }
//}