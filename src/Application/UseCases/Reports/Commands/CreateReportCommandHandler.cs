using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;
using Application.UseCases.Reports.Specs;

namespace Application.UseCases.Reports.Commands;

public class CreateReportCommandHandler
    : IRequestHandler<CreateReportCommand, Guid>
{
    private readonly IWriteRepository<ContentReport, Guid> _writeRepo;
    private readonly IReadRepository<ContentReport, Guid> _reportRepo;
    private readonly IReadRepository<User, Guid> _userRepo;
    private readonly IReadRepository<DiscussionComment, Guid> _commentRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public CreateReportCommandHandler(
        IWriteRepository<ContentReport, Guid> writeRepo,
        IReadRepository<ContentReport, Guid> reportRepo,
        IReadRepository<User, Guid> userRepo,
        IReadRepository<DiscussionComment, Guid> commentRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        _writeRepo = writeRepo;
        _reportRepo = reportRepo;
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

        // 🔥 Anti duplicate
        var existed = await _reportRepo.FirstOrDefaultAsync(
            new ReportByUserAndTargetSpec(userId, request.TargetId, "comment"), ct);

        if (existed != null)
            throw new Exception("Already reported");

        var report = new ContentReport
        {
            Id = Guid.NewGuid(),
            ReporterId = userId,
            TargetId = request.TargetId,
            TargetType = "comment",
            Reason = request.Reason.Trim(),
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        await _writeRepo.AddAsync(report, ct);

        // 🔥 COUNT pending reports (KHÔNG cần spec mới)
        var count = await _reportRepo.CountAsync(
            new ReportsByTargetAndStatusSpec(request.TargetId, "comment", "pending"), ct);

        // 🔥 Auto hide nếu >=3
        if (count + 1 >= 3)
        {
            comment.IsHidden = true;
        }

        await _uow.SaveChangesAsync(ct);

        return report.Id;
    }
}