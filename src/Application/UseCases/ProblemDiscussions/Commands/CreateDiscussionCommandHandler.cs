using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.ProblemDiscussions.Commands;

public class CreateDiscussionCommandHandler
    : IRequestHandler<CreateDiscussionCommand, Guid>
{
    private readonly IWriteRepository<ProblemDiscussion, Guid> _writeRepo;
    private readonly IReadRepository<Problem, Guid> _problemRepo;
    private readonly IReadRepository<User, Guid> _userRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CreateDiscussionCommandHandler> _logger;

    public CreateDiscussionCommandHandler(
        IWriteRepository<ProblemDiscussion, Guid> writeRepo,
        IReadRepository<Problem, Guid> problemRepo,
        IReadRepository<User, Guid> userRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        ILogger<CreateDiscussionCommandHandler> logger)
    {
        _writeRepo = writeRepo;
        _problemRepo = problemRepo;
        _userRepo = userRepo;
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateDiscussionCommand request, CancellationToken ct)
    {
        // 1. Validate input
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        if (string.IsNullOrWhiteSpace(request.Content))
            throw new ArgumentException("Content is required.");

        // 2. Lấy userId từ JWT
        var userId = _currentUser.UserId;
        if (userId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        _logger.LogInformation("Creating discussion: ProblemId={ProblemId}, UserId={UserId}", request.ProblemId, userId);

        // 3. Kiểm tra problem tồn tại
        var problem = await _problemRepo.GetByIdAsync(request.ProblemId, ct);
        if (problem is null || !problem.IsActive)
            throw new Exception("Problem not found.");

        // 4. Kiểm tra user tồn tại
        var user = await _userRepo.GetByIdAsync(userId.Value, ct);
        if (user is null)
            throw new Exception("User not found.");

        _logger.LogInformation("User found: {DisplayName}, Problem found: {Title}", user.DisplayName, problem.Title);

        // 5. Tạo discussion - CHỈ set các scalar fields, KHÔNG set navigation properties
        // Npgsql 6.0+: column "timestamp without time zone" yêu cầu DateTimeKind.Unspecified
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        var discussion = new ProblemDiscussion
        {
            Id = Guid.NewGuid(),
            ProblemId = request.ProblemId,
            UserId = userId.Value,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            IsPinned = false,
            IsLocked = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        try
        {
            await _writeRepo.AddAsync(discussion, ct);
            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("Discussion created successfully: {DiscussionId}", discussion.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save discussion. InnerException: {Inner}", ex.InnerException?.Message);
            throw;
        }

        return discussion.Id;
    }
}
