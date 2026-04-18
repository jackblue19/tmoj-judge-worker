using Application.Common.Interfaces;
using Application.UseCases.StudyPlans.Commands.CreateStudyPlan;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

public class CreateStudyPlanHandler : IRequestHandler<CreateStudyPlanCommand, Guid>
{
    private readonly IStudyPlanRepository _repo;
    private readonly ILogger<CreateStudyPlanHandler> _logger;
    private readonly IHttpContextAccessor _httpContext;

    public CreateStudyPlanHandler(
        IStudyPlanRepository repo,
        ILogger<CreateStudyPlanHandler> logger,
        IHttpContextAccessor httpContext)
    {
        _repo = repo;
        _logger = logger;
        _httpContext = httpContext;
    }

    public async Task<Guid> Handle(CreateStudyPlanCommand request, CancellationToken ct)
    {
        try
        {
            // 🔥 LẤY USER ID TỪ JWT
            var userIdStr = _httpContext.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdStr))
            {
                _logger.LogError("❌ UserId not found in token");
                throw new UnauthorizedAccessException("User not authenticated");
            }

            var userId = Guid.Parse(userIdStr);

            _logger.LogInformation("🚀 Creating StudyPlan by User: {UserId}", userId);

            var plan = new StudyPlan
            {
                Id = Guid.NewGuid(),
                CreatorId = userId, // ✅ FIX QUAN TRỌNG
                Title = request.Title,
                Description = request.Description,
                IsPublic = request.IsPublic,
                IsPaid = request.IsPaid,
                Price = request.Price,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
            };

            var id = await _repo.CreateAsync(plan);

            await _repo.SaveChangesAsync();

            _logger.LogInformation("✅ StudyPlan created: {Id}", id);

            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ ERROR CreateStudyPlan");
            throw;
        }
    }
}