using Domain.Abstractions;
using Domain.Entities;
using Application.UseCases.Reports.Specs;

namespace Application.Common.Services;

public class AutoModerationService
{
    private readonly IReadRepository<ContentReport, Guid> _reportRepo;
    private readonly IReadRepository<DiscussionComment, Guid> _commentRepo;
    private readonly IWriteRepository<DiscussionComment, Guid> _commentWriteRepo;
    private readonly IWriteRepository<ModerationAction, Guid> _actionRepo;
    private readonly IUnitOfWork _uow;

    private const int REPORT_THRESHOLD = 3;

    public AutoModerationService(
        IReadRepository<ContentReport, Guid> reportRepo,
        IReadRepository<DiscussionComment, Guid> commentRepo,
        IWriteRepository<DiscussionComment, Guid> commentWriteRepo,
        IWriteRepository<ModerationAction, Guid> actionRepo,
        IUnitOfWork uow)
    {
        _reportRepo = reportRepo;
        _commentRepo = commentRepo;
        _commentWriteRepo = commentWriteRepo;
        _actionRepo = actionRepo;
        _uow = uow;
    }

    public async Task CheckAndModerateAsync(Guid targetId, string targetType, CancellationToken ct)
    {
        // 🔥
        var reportCount = await _reportRepo.CountAsync(
            new ReportsByTargetAndStatusSpec(targetId, targetType, "pending"), ct);

        if (reportCount < REPORT_THRESHOLD)
            return;

        // 🔥 chỉ xử lý comment
        if (string.Equals(targetType, "comment", StringComparison.OrdinalIgnoreCase))
        {
            var comment = await _commentRepo.GetByIdAsync(targetId, ct);
            if (comment == null) return;

            // 🔥 hide comment
            comment.IsHidden = true;
            _commentWriteRepo.Update(comment);

            // 🔥 log system action
            await _actionRepo.AddAsync(new ModerationAction
            {
                Id = Guid.NewGuid(),
                ReportId = Guid.Empty,
                AdminId = Guid.Empty,
                ActionType = "AUTO_HIDE",
                Note = $"Auto hidden due to {reportCount} reports",
                CreatedAt = DateTime.UtcNow
            }, ct);

            await _uow.SaveChangesAsync(ct);
        }
    }
}