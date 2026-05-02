using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Application.Common;
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
    private readonly ILogger<VoteCommentCommandHandler> _logger;

    public VoteCommentCommandHandler(
        IReadRepository<DiscussionComment, Guid> commentReadRepo,
        IWriteRepository<DiscussionComment, Guid> commentWriteRepo,
        IReadRepository<ContentVote, Guid> voteReadRepo,
        IWriteRepository<ContentVote, Guid> voteWriteRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        ILogger<VoteCommentCommandHandler> logger)
    {
        _commentReadRepo = commentReadRepo;
        _commentWriteRepo = commentWriteRepo;
        _voteReadRepo = voteReadRepo;
        _voteWriteRepo = voteWriteRepo;
        _currentUser = currentUser;
        _uow = uow;
        _logger = logger;
    }

    public async Task<bool> Handle(VoteCommentCommand request, CancellationToken ct)
    {
        try
        {
            if (request.VoteType is not (1 or -1 or 0))
                throw new ArgumentException("VoteType must be 1 (upvote), -1 (downvote), or 0 (unvote).");

            var userId = _currentUser.UserId;
            if (userId is null || userId == Guid.Empty)
                throw new UnauthorizedAccessException();

            // check comment tồn tại
            var comment = await _commentReadRepo.GetByIdAsync(request.CommentId, ct);

            if (comment == null)
                throw new NotFoundException("Comment not found");

            // ❌ Không được vote bài của mình
            if (comment.UserId == userId.Value)
                throw new ConflictException("You cannot vote your own comment");

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
            if (existingVote != null && newVoteValue != 0)
            {
                existingVote.Vote = (short)newVoteValue;
                existingVote.CreatedAt = DateTime.UtcNow; // UTC for 'timestamp with time zone'
                _voteWriteRepo.Update(existingVote);
            }
            else if (existingVote != null && newVoteValue == 0)
            {
                // Remove vote (Toggle)
                _voteWriteRepo.Remove(existingVote);
            }
            else if (existingVote == null && newVoteValue != 0)
            {
                var vote = new ContentVote
                {
                    Id = Guid.NewGuid(),
                    UserId = userId.Value,
                    TargetId = request.CommentId,
                    TargetType = "comment",
                    Vote = (short)newVoteValue,
                    CreatedAt = DateTime.UtcNow // UTC for 'timestamp with time zone'
                };
                await _voteWriteRepo.AddAsync(vote, ct);
            }

            await _uow.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VoteCommentCommandHandler ERROR | CommentId={CommentId}, UserId={UserId}", 
                request.CommentId, _currentUser.UserId);
            throw;
        }
    }
}