using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;

namespace Application.UseCases.DiscussionComments.Commands;

public class CreateDiscussionCommentCommandHandler
    : IRequestHandler<CreateDiscussionCommentCommand, Guid>
{
    private readonly IWriteRepository<DiscussionComment, Guid> _writeRepo;
    private readonly IReadRepository<ProblemDiscussion, Guid> _discussionRepo;
    private readonly IDiscussionCommentRepository _commentRepo; 
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public CreateDiscussionCommentCommandHandler(
        IWriteRepository<DiscussionComment, Guid> writeRepo,
        IReadRepository<ProblemDiscussion, Guid> discussionRepo,
        IDiscussionCommentRepository commentRepo, 
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _writeRepo = writeRepo;
        _discussionRepo = discussionRepo;
        _commentRepo = commentRepo; 
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateDiscussionCommentCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;

        if (!userId.HasValue)
            throw new UnauthorizedAccessException();

        // ===============================
        // CHECK DISCUSSION
        // ===============================
        var discussion = await _discussionRepo.GetByIdAsync(request.DiscussionId, ct);
        if (discussion == null)
            throw new Exception("Discussion not found");

        // ===============================
        // VALIDATE PARENT + DEPTH
        // ===============================
        if (request.ParentId.HasValue)
        {
            var parent = await _commentRepo.GetByIdWithUserAsync(request.ParentId.Value);

            if (parent == null)
                throw new Exception("Parent comment not found");

            if (parent.DiscussionId != request.DiscussionId)
                throw new Exception("Parent comment does not belong to this discussion");

            // 🔥 CHECK MAX DEPTH = 3
            if (parent.ParentId != null)
            {
                var grandParent = await _commentRepo.GetByIdWithUserAsync(parent.ParentId.Value);

                if (grandParent?.ParentId != null)
                    throw new Exception("Max comment depth is 3");
            }
        }

        // ===============================
        // CREATE COMMENT
        // ===============================
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        var comment = new DiscussionComment
        {
            Id = Guid.NewGuid(),
            DiscussionId = request.DiscussionId,
            UserId = userId.Value,
            Content = request.Content.Trim(),
            ParentId = request.ParentId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _writeRepo.AddAsync(comment, ct);
        await _uow.SaveChangesAsync(ct);

        return comment.Id;
    }
}