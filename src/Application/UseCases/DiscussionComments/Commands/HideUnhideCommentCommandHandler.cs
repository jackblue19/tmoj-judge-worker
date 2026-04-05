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

    public HideUnhideCommentCommandHandler(
        IReadRepository<DiscussionComment, Guid> commentRepo,
        IWriteRepository<DiscussionComment, Guid> commentWriteRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _commentRepo = commentRepo;
        _commentWriteRepo = commentWriteRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<bool> Handle(HideUnhideCommentCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var comment = await _commentRepo.GetByIdAsync(request.CommentId, ct)
                      ?? throw new Exception("Comment not found");

        // Chỉ chủ comment hoặc admin/manager mới được hide/unhide
        if (comment.UserId != userId && !_currentUser.IsInRole("admin") && !_currentUser.IsInRole("manager"))
            throw new UnauthorizedAccessException("You are not allowed to modify this comment visibility");

        comment.IsHidden = request.Hide;
        _commentWriteRepo.Update(comment);

        await _uow.SaveChangesAsync(ct);
        return true;
    }
}