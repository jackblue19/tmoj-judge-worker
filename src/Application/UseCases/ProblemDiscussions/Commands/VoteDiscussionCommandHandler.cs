using Application.Common.Interfaces;
using Application.UseCases.ProblemDiscussions.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.ProblemDiscussions.Commands;

public class VoteDiscussionCommandHandler
    : IRequestHandler<VoteDiscussionCommand, bool>
{
    private readonly IProblemDiscussionRepository _discussionRepo;
    private readonly IReadRepository<ContentVote, Guid> _voteReadRepo;
    private readonly IWriteRepository<ContentVote, Guid> _voteWriteRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;



    public VoteDiscussionCommandHandler(
        IProblemDiscussionRepository discussionRepo,
        IReadRepository<ContentVote, Guid> voteReadRepo,
        IWriteRepository<ContentVote, Guid> voteWriteRepo,
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
        var discussion = await _discussionRepo.GetEntityByIdAsync(request.DiscussionId);
        if (discussion == null)
            throw new Exception("Discussion not found");

        // ❌ Không được vote bài của mình
        if (discussion.UserId == userId.Value)
            throw new Exception("You cannot vote your own discussion");

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
        if (newVoteValue == 0)
        {
            if (existingVote != null)
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
                    CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                };
                await _voteWriteRepo.AddAsync(vote, ct);
            }
            else
            {
                existingVote.Vote = (short)newVoteValue;
                existingVote.CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                _voteWriteRepo.Update(existingVote);
            }
        }

        await _uow.SaveChangesAsync(ct);
        return true;
    }
}