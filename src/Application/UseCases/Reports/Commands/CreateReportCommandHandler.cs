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
    private readonly IReadRepository<ContentReport, Guid> _reportRepo; // 🔥 thêm
    private readonly IReadRepository<User, Guid> _userRepo;
    private readonly IReadRepository<DiscussionComment, Guid> _commentRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public CreateReportCommandHandler(
        IWriteRepository<ContentReport, Guid> writeRepo,
        IReadRepository<ContentReport, Guid> reportRepo, // 🔥 thêm
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
        // 🔥 1. Validate input
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Reason is required");

        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        // 🔥 2. Check user tồn tại
        var user = await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new Exception("User not found");

        // 🔥 3. Check comment tồn tại
        var comment = await _commentRepo.GetByIdAsync(request.TargetId, ct)
            ?? throw new Exception("Comment not found");

        // 🔥 4. Không được report chính mình
        if (comment.UserId == userId)
            throw new Exception("Cannot report your own comment");

        // 🔥 5. ANTI-SPAM (quan trọng)
        var spec = new ReportByUserAndTargetSpec(
            userId,
            request.TargetId,
            "comment");

        var existed = await _reportRepo.FirstOrDefaultAsync(spec, ct);

        if (existed != null)
            throw new Exception("You already reported this content");

        // 🔥 6. Create report
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        var report = new ContentReport
        {
            Id = Guid.NewGuid(),
            ReporterId = userId,
            TargetId = request.TargetId,
            TargetType = "comment",   // 🔥 normalize
            Reason = request.Reason.Trim(),
            Status = "pending",       // 🔥 normalize
            CreatedAt = now
        };

        await _writeRepo.AddAsync(report, ct);
        await _uow.SaveChangesAsync(ct);

        return report.Id;
    }
}