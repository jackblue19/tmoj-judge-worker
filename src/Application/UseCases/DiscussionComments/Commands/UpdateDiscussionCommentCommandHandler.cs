using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;

namespace Application.UseCases.DiscussionComments.Commands;

public class UpdateDiscussionCommentCommandHandler
    : IRequestHandler<UpdateDiscussionCommentCommand, bool>
{
    private readonly IWriteRepository<DiscussionComment, Guid> _writeRepo;
    private readonly IReadRepository<DiscussionComment, Guid> _readRepo;
    private readonly IReadRepository<ProblemDiscussion, Guid> _discussionRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public UpdateDiscussionCommentCommandHandler(
        IWriteRepository<DiscussionComment, Guid> writeRepo,
        IReadRepository<DiscussionComment, Guid> readRepo,
        IReadRepository<ProblemDiscussion, Guid> discussionRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _writeRepo = writeRepo;
        _readRepo = readRepo;
        _discussionRepo = discussionRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateDiscussionCommentCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            throw new UnauthorizedAccessException();

        var comment = await _readRepo.GetByIdAsync(request.CommentId, ct);
        if (comment == null)
            throw new Exception("Comment not found");

        // Check permission: comment owner, discussion owner, or admin/manager
        var isCommentOwner = comment.UserId == userId.Value;
        var isAdmin = _currentUser.IsInRole("admin") || _currentUser.IsInRole("manager");

        if (!isCommentOwner && !isAdmin)
        {
            var discussion = await _discussionRepo.GetByIdAsync(comment.DiscussionId, ct);
            var isDiscussionOwner = discussion?.UserId == userId.Value;

            if (!isDiscussionOwner)
                throw new UnauthorizedAccessException("You are not allowed to edit this comment");
        }

        comment.Content = request.Content.Trim();
        comment.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        _writeRepo.Update(comment);
        await _uow.SaveChangesAsync(ct);

        return true;
    }
}