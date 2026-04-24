using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;

namespace Application.UseCases.DiscussionComments.Commands;

public class HideUnhideCommentCommandHandler : IRequestHandler<HideUnhideCommentCommand, bool>
{
    private readonly IReadRepository<DiscussionComment, Guid> _commentRepo;
    private readonly IWriteRepository<DiscussionComment, Guid> _commentWriteRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly IReadRepository<ContentReport, Guid> _reportRepo;

    public HideUnhideCommentCommandHandler(
        IReadRepository<DiscussionComment, Guid> commentRepo,
        IWriteRepository<DiscussionComment, Guid> commentWriteRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        IReadRepository<ContentReport, Guid> reportRepo)
    {
        _commentRepo = commentRepo;
        _commentWriteRepo = commentWriteRepo;
        _currentUser = currentUser;
        _uow = uow;
        _reportRepo = reportRepo;
    }

    public async Task<bool> Handle(HideUnhideCommentCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var comment = await _commentRepo.GetByIdAsync(request.CommentId, ct)
                      ?? throw new Exception("Comment not found");

        var isAdmin = _currentUser.IsInRole("admin") || _currentUser.IsInRole("manager");

        // Chỉ chủ comment hoặc admin/manager mới được hide/unhide
        if (comment.UserId != userId && !isAdmin)
            throw new UnauthorizedAccessException("You are not allowed to modify this comment visibility");

        // Không cho phép user tự ý unhide nếu comment này đã bị admin ẩn do vi phạm (có report approved)
        if (!request.Hide && !isAdmin)
        {
            var approvedReportsCount = await _reportRepo.CountAsync(
                new Application.UseCases.Reports.Specs.ApprovedReportCountSpec(comment.Id, "comment"), ct);

            if (approvedReportsCount > 0)
                throw new Exception("This comment has been hidden by moderation and cannot be unhidden.");
        }

        comment.IsHidden = request.Hide;
        _commentWriteRepo.Update(comment);

        await _uow.SaveChangesAsync(ct);
        return true;
    }
}