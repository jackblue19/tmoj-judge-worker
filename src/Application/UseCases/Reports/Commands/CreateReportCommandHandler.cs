using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;

namespace Application.UseCases.Reports.Commands;

public class CreateReportCommandHandler
    : IRequestHandler<CreateReportCommand, Guid>
{
    private readonly IWriteRepository<ContentReport, Guid> _writeRepo;
    private readonly IReadRepository<User, Guid> _userRepo;
    private readonly IReadRepository<DiscussionComment, Guid> _commentRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public CreateReportCommandHandler(
        IWriteRepository<ContentReport, Guid> writeRepo,
        IReadRepository<User, Guid> userRepo,
        IReadRepository<DiscussionComment, Guid> commentRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        _writeRepo = writeRepo;
        _userRepo = userRepo;
        _commentRepo = commentRepo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateReportCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Reason is required");

        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var user = await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new Exception("User not found");

        var comment = await _commentRepo.GetByIdAsync(request.TargetId, ct)
            ?? throw new Exception("Comment not found");

        if (comment.UserId == userId)
            throw new Exception("Cannot report your own comment");

        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        var report = new ContentReport
        {
            Id = Guid.NewGuid(),
            ReporterId = userId,
            TargetId = request.TargetId,
            TargetType = "Comment",
            Reason = request.Reason.Trim(),
            Status = "Pending",
            CreatedAt = now
        };

        await _writeRepo.AddAsync(report, ct);
        await _uow.SaveChangesAsync(ct);

        return report.Id;
    }
}