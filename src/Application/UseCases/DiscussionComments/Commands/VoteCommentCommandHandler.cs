using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;
using Application.UseCases.DiscussionComments.Specs;

namespace Application.UseCases.DiscussionComments.Commands;

public class VoteCommentCommandHandler
    : IRequestHandler<VoteCommentCommand, bool>
{
    private readonly IReadRepository<DiscussionComment, Guid> _commentRepo;
    private readonly IWriteRepository<DiscussionComment, Guid> _commentWriteRepo;
    private readonly IReadRepository<CommentVote, Guid> _voteReadRepo;
    private readonly IWriteRepository<CommentVote, Guid> _voteWriteRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public VoteCommentCommandHandler(
        IReadRepository<DiscussionComment, Guid> commentRepo,
        IWriteRepository<DiscussionComment, Guid> commentWriteRepo,
        IReadRepository<CommentVote, Guid> voteReadRepo,
        IWriteRepository<CommentVote, Guid> voteWriteRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _commentRepo = commentRepo;
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

        var comment = await _commentRepo.GetByIdAsync(request.CommentId, ct)
                      ?? throw new Exception("Comment not found");

        var existingVote = await _voteReadRepo.FirstOrDefaultAsync(
            new CommentVoteByUserAndCommentSpec(userId.Value, request.CommentId), ct);

        // =========================
        // CASE 1: UNVOTE
        // =========================
        if (request.VoteType == 0)
        {
            if (existingVote != null)
            {
                comment.VoteCount -= existingVote.Vote;
                _voteWriteRepo.Remove(existingVote);
            }

            _commentWriteRepo.Update(comment);
            await _uow.SaveChangesAsync(ct);
            return true;
        }

        // =========================
        // CASE 2: FIRST TIME VOTE
        // =========================
        if (existingVote == null)
        {
            var vote = new CommentVote
            {
                Id = Guid.NewGuid(),
                CommentId = request.CommentId,
                UserId = userId.Value,
                Vote = (short)request.VoteType,
                CreatedAt = DateTime.UtcNow,
            };

            await _voteWriteRepo.AddAsync(vote, ct);
            comment.VoteCount += request.VoteType;

            _commentWriteRepo.Update(comment);
            await _uow.SaveChangesAsync(ct);
            return true;
        }

        // =========================
        // CASE 3: TOGGLE / CHANGE VOTE
        // =========================
        if (existingVote.Vote == request.VoteType)
        {
            // user click lại cùng loại => unvote
            comment.VoteCount -= existingVote.Vote;
            _voteWriteRepo.Remove(existingVote);
        }
        else
        {
            // đổi từ up -> down hoặc ngược lại
            comment.VoteCount -= existingVote.Vote;
            existingVote.Vote = (short)request.VoteType;
            comment.VoteCount += request.VoteType;

            _voteWriteRepo.Update(existingVote);
        }

        _commentWriteRepo.Update(comment);
        await _uow.SaveChangesAsync(ct);

        return true;
    }
}