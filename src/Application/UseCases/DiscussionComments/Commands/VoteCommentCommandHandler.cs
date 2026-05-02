using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;
using Application.UseCases.DiscussionComments.Specs;

namespace Application.UseCases.DiscussionComments.Commands;

public class VoteCommentCommandHandler
    : IRequestHandler<VoteCommentCommand, bool>
{
    private readonly IReadRepository<DiscussionComment, Guid> _commentReadRepo;
    private readonly IWriteRepository<DiscussionComment, Guid> _commentWriteRepo;
    private readonly IReadRepository<ContentVote, Guid> _voteReadRepo;
    private readonly IWriteRepository<ContentVote, Guid> _voteWriteRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public VoteCommentCommandHandler(
        IReadRepository<DiscussionComment, Guid> commentReadRepo,
        IWriteRepository<DiscussionComment, Guid> commentWriteRepo,
        IReadRepository<ContentVote, Guid> voteReadRepo,
        IWriteRepository<ContentVote, Guid> voteWriteRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _commentReadRepo = commentReadRepo;
        _commentWriteRepo = commentWriteRepo;
        _voteReadRepo = voteReadRepo;
        _voteWriteRepo = voteWriteRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<bool> Handle(VoteCommentCommand request, CancellationToken ct)
    {
        if (request.VoteType is not (1 or -1 or 0))
            throw new ArgumentException("VoteType must be 1 (upvote), -1 (downvote), or 0 (unvote).");

        var userId = _currentUser.UserId;
        if (userId is null || userId == Guid.Empty)
            throw new UnauthorizedAccessException();

        // check comment tồn tại
        var comment = await _commentReadRepo.GetByIdAsync(request.CommentId, ct);

        if (comment == null)
            throw new Exception("Comment not found");

        var existingVote = await _voteReadRepo.FirstOrDefaultAsync(
      new CommentUserVoteSingleSpec(userId.Value, request.CommentId), ct);

        int oldVoteValue = existingVote?.Vote ?? 0;
        int newVoteValue = request.VoteType;

        // Toggle logic: if same vote type, unvote
        if (existingVote != null && existingVote.Vote == request.VoteType)
        {
            newVoteValue = 0;
        }

        // =========================
        // UPDATE SCORE
        // =========================
        comment.VoteCount += (newVoteValue - oldVoteValue);
        comment.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        _commentWriteRepo.Update(comment);

        // =========================
        // SAVE VOTE RECORD
        // =========================
        if (newVoteValue == 0)
        {
            if (existingVote != null)
                _voteWriteRepo.Remove(existingVote);
        }
        else if (existingVote == null)
        {
            var vote = new ContentVote
            {
                Id = Guid.NewGuid(),
                TargetId = request.CommentId,
                TargetType = "comment",
                UserId = userId.Value,
                Vote = (short)newVoteValue,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
            };
            await _voteWriteRepo.AddAsync(vote, ct);
        }
        else
        {
            existingVote.Vote = (short)newVoteValue;
            existingVote.CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            _voteWriteRepo.Update(existingVote);
        }

        await _uow.SaveChangesAsync(ct);
        return true;
    }
}