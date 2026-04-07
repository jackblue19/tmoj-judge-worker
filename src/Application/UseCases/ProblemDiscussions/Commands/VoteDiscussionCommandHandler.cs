using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;
using Application.UseCases.ProblemDiscussions.Specs;

namespace Application.UseCases.ProblemDiscussions.Commands;

public class VoteDiscussionCommandHandler
    : IRequestHandler<VoteDiscussionCommand, bool>
{
    private readonly IReadRepository<ProblemDiscussion, Guid> _discussionRepo;
    private readonly IReadRepository<CommentVote, Guid> _voteReadRepo;
    private readonly IWriteRepository<CommentVote, Guid> _voteWriteRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public VoteDiscussionCommandHandler(
        IReadRepository<ProblemDiscussion, Guid> discussionRepo,
        IReadRepository<CommentVote, Guid> voteReadRepo,
        IWriteRepository<CommentVote, Guid> voteWriteRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _discussionRepo = discussionRepo;
        _voteReadRepo = voteReadRepo;
        _voteWriteRepo = voteWriteRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<bool> Handle(VoteDiscussionCommand request, CancellationToken ct)
    {
        if (request.VoteType is not (1 or -1 or 0))
            throw new ArgumentException("VoteType must be 1, -1 or 0");

        var userId = _currentUser.UserId;
        if (userId == null || userId == Guid.Empty)
            throw new UnauthorizedAccessException();

        var discussion = await _discussionRepo.GetByIdAsync(request.DiscussionId, ct)
                         ?? throw new Exception("Discussion not found");

        // ❗ BLOCK SELF VOTE
        if (discussion.UserId == userId.Value)
            throw new Exception("You cannot vote your own discussion");

        var existingVote = await _voteReadRepo.FirstOrDefaultAsync(
            new DiscussionVoteSpec(userId.Value, request.DiscussionId), ct);

        // UNVOTE
        if (request.VoteType == 0)
        {
            if (existingVote != null)
                _voteWriteRepo.Remove(existingVote);

            await _uow.SaveChangesAsync(ct);
            return true;
        }

        // FIRST VOTE
        if (existingVote == null)
        {
            var vote = new CommentVote
            {
                Id = Guid.NewGuid(),
                CommentId = request.DiscussionId,
                UserId = userId.Value,
                Vote = (short)request.VoteType,
                CreatedAt = DateTime.UtcNow
            };

            await _voteWriteRepo.AddAsync(vote, ct);
            await _uow.SaveChangesAsync(ct);
            return true;
        }

        // TOGGLE
        if (existingVote.Vote == request.VoteType)
        {
            _voteWriteRepo.Remove(existingVote);
        }
        else
        {
            existingVote.Vote = (short)request.VoteType;
            _voteWriteRepo.Update(existingVote);
        }

        await _uow.SaveChangesAsync(ct);
        return true;
    }
}