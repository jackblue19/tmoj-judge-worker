using Application.Common.Interfaces;
using Application.UseCases.ProblemDiscussions.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.ProblemDiscussions.Commands;

public class VoteDiscussionCommandHandler
    : IRequestHandler<VoteDiscussionCommand, bool>
{
    private readonly IReadRepository<ProblemDiscussion, Guid> _discussionRepo;
    private readonly IReadRepository<ContentReport, Guid> _voteReadRepo;
    private readonly IWriteRepository<ContentReport, Guid> _voteWriteRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    private const string VOTE_TYPE = "discussion_vote";

    public VoteDiscussionCommandHandler(
        IReadRepository<ProblemDiscussion, Guid> discussionRepo,
        IReadRepository<ContentReport, Guid> voteReadRepo,
        IWriteRepository<ContentReport, Guid> voteWriteRepo,
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
        var userId = _currentUser.UserId;
        if (userId is null)
            throw new UnauthorizedAccessException("User not authenticated");

        // ===============================
        // GET DISCUSSION
        // ===============================
        var discussion = await _discussionRepo.GetByIdAsync(request.DiscussionId, ct);
        if (discussion == null)
            throw new Exception("Discussion not found");

        // ❌ Không được vote bài của mình
        if (discussion.UserId == userId.Value)
            throw new Exception("You cannot vote your own discussion");

        // ===============================
        // FIND EXISTING VOTE (SPEC)
        // ===============================
        var spec = new DiscussionVoteByUserSpec(userId.Value, request.DiscussionId);

        var existingVote = (await _voteReadRepo.ListAsync(spec, ct))
            .FirstOrDefault();

        // ===============================
        // UNVOTE
        // ===============================
        if (request.VoteType == 0)
        {
            if (existingVote != null)
                _voteWriteRepo.Remove(existingVote);

            await _uow.SaveChangesAsync(ct);
            return true;
        }

        // ===============================
        // NEW VOTE
        // ===============================
        if (existingVote == null)
        {
            var vote = new ContentReport
            {
                Id = Guid.NewGuid(),
                ReporterId = userId.Value,
                TargetId = request.DiscussionId,
                TargetType = VOTE_TYPE,
                Reason = request.VoteType.ToString(),
                Status = "pending", 
                CreatedAt = DateTime.UtcNow
            };

            await _voteWriteRepo.AddAsync(vote, ct);
        }
        else
        {
            // ===============================
            // TOGGLE LOGIC
            // ===============================
            if (existingVote.Reason == request.VoteType.ToString())
            {
                _voteWriteRepo.Remove(existingVote);
            }
            else
            {
                existingVote.Reason = request.VoteType.ToString();
                existingVote.CreatedAt = DateTime.UtcNow;

                _voteWriteRepo.Update(existingVote);
            }
        }

        await _uow.SaveChangesAsync(ct);
        return true;
    }
}