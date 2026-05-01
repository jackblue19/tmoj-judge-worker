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
    private readonly IReadRepository<ContentVote, Guid> _voteReadRepo;
    private readonly IWriteRepository<ContentVote, Guid> _voteWriteRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public VoteCommentCommandHandler(
        IReadRepository<DiscussionComment, Guid> commentRepo,
        IReadRepository<ContentVote, Guid> voteReadRepo,
        IWriteRepository<ContentVote, Guid> voteWriteRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _commentRepo = commentRepo;
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
        var commentExists = await _commentRepo.AnyAsync(
            new CommentByIdSpec(request.CommentId), ct);

        if (!commentExists)
            throw new Exception("Comment not found");

        var existingVote = await _voteReadRepo.FirstOrDefaultAsync(
      new CommentUserVoteSingleSpec(userId.Value, request.CommentId), ct);

        // =========================
        // CASE 1: UNVOTE
        // =========================
        if (request.VoteType == 0)
        {
            if (existingVote != null)
            {
                _voteWriteRepo.Remove(existingVote);
                await _uow.SaveChangesAsync(ct);
            }

            return true;
        }

        // =========================
        // CASE 2: FIRST TIME VOTE
        // =========================
        if (existingVote == null)
        {
            var vote = new ContentVote
            {
                Id = Guid.NewGuid(),
                TargetId = request.CommentId,
                TargetType = "comment",
                UserId = userId.Value,
                Vote = (short)request.VoteType,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
            };

            await _voteWriteRepo.AddAsync(vote, ct);
            await _uow.SaveChangesAsync(ct);

            return true;
        }

        // =========================
        // CASE 3: TOGGLE / CHANGE VOTE
        // =========================
        if (existingVote.Vote == request.VoteType)
        {
            // click lại => unvote
            _voteWriteRepo.Remove(existingVote);
        }
        else
        {
            // đổi vote
            existingVote.Vote = (short)request.VoteType;
            _voteWriteRepo.Update(existingVote);
        }

        await _uow.SaveChangesAsync(ct);

        return true;
    }
}