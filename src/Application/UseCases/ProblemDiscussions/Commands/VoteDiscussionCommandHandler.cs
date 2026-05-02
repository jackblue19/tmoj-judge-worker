using Application.Common;
using Application.Common.Interfaces;
using Application.UseCases.ProblemDiscussions.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.ProblemDiscussions.Commands;

public class VoteDiscussionCommandHandler
    : IRequestHandler<VoteDiscussionCommand, bool>
{
    private readonly IProblemDiscussionRepository _discussionRepo;
    private readonly IReadRepository<ContentVote, Guid> _voteReadRepo;
    private readonly IWriteRepository<ContentVote, Guid> _voteWriteRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<VoteDiscussionCommandHandler> _logger;



    public VoteDiscussionCommandHandler(
        IProblemDiscussionRepository discussionRepo,
        IReadRepository<ContentVote, Guid> voteReadRepo,
        IWriteRepository<ContentVote, Guid> voteWriteRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        ILogger<VoteDiscussionCommandHandler> logger)
    {
        _discussionRepo = discussionRepo;
        _voteReadRepo = voteReadRepo;
        _voteWriteRepo = voteWriteRepo;
        _currentUser = currentUser;
        _uow = uow;
        _logger = logger;
    }

    public async Task<bool> Handle(VoteDiscussionCommand request, CancellationToken ct)
    {
        try
        {
            var userId = _currentUser.UserId;
            if (userId is null)
                throw new UnauthorizedAccessException("User not authenticated");

            // ===============================
            // GET DISCUSSION 
            // ===============================
            var discussion = await _discussionRepo.GetEntityByIdAsync(request.DiscussionId);
            if (discussion == null)
                throw new NotFoundException("Discussion not found");

            // ❌ Không được vote bài của mình
            if (discussion.UserId == userId.Value)
                throw new ConflictException("You cannot vote your own discussion");

            // ===============================
            // FIND EXISTING VOTE
            // ===============================
            var spec = new DiscussionVoteByUserSpec(userId.Value, request.DiscussionId);
            var existingVote = (await _voteReadRepo.ListAsync(spec, ct)).FirstOrDefault();

            int oldVoteValue = 0;
            if (existingVote != null)
            {
                oldVoteValue = existingVote.Vote;
            }

            int newVoteValue = request.VoteType;

            // Nếu vote giống hệt cũ thì coi như muốn gỡ vote (Toggle)
            if (existingVote != null && oldVoteValue == newVoteValue)
            {
                newVoteValue = 0;
            }

            // ===============================
            // UPDATE DISCUSSION SCORE
            // ===============================
            // Hiệu số thay đổi: (Giá trị mới) - (Giá trị cũ)
            discussion.VoteCount += (newVoteValue - oldVoteValue);

            // ===============================
            // SAVE VOTE RECORD
            // ===============================
            if (existingVote != null && newVoteValue != 0)
            {
                existingVote.Vote = (short)newVoteValue;
                existingVote.CreatedAt = DateTime.UtcNow; // UTC for 'timestamp with time zone'
                _voteWriteRepo.Update(existingVote);
            }
            else if (existingVote != null && newVoteValue == 0)
            {
                // Remove vote
                _voteWriteRepo.Remove(existingVote);
            }
            else
            {
                if (existingVote == null)
                {
                    var vote = new ContentVote
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId.Value,
                        TargetId = request.DiscussionId,
                        TargetType = "discussion",
                        Vote = (short)newVoteValue,
                        CreatedAt = DateTime.UtcNow // UTC for 'timestamp with time zone'
                    };
                    await _voteWriteRepo.AddAsync(vote, ct);
                }
            }

            // Track Update
            // ProblemDiscussion.UpdatedAt is 'timestamp without time zone' in TmojDbContext
            discussion.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

            await _uow.SaveChangesAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VoteDiscussionCommandHandler ERROR | DiscussionId={DiscussionId}, UserId={UserId}", 
                request.DiscussionId, _currentUser.UserId);
            throw;
        }
    }
}