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
    private readonly IReadRepository<ContentReport, Guid> _voteReadRepo;
    private readonly IWriteRepository<ContentReport, Guid> _voteWriteRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    private const string VOTE_TYPE = "discussion_vote";

    public VoteDiscussionCommandHandler(
        IProblemDiscussionRepository discussionRepo,
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
            int.TryParse(existingVote.Reason, out oldVoteValue);
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
                var vote = new ContentReport
                {
                    Id = Guid.NewGuid(),
                    ReporterId = userId.Value,
                    TargetId = request.DiscussionId,
                    TargetType = VOTE_TYPE,
                    Reason = newVoteValue.ToString(),
                    Status = "voted", 
                    CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
                };
                await _voteWriteRepo.AddAsync(vote, ct);
            }
            else
            {
                existingVote.Reason = newVoteValue.ToString();
                existingVote.CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
                _voteWriteRepo.Update(existingVote);
            }
        }

        await _uow.SaveChangesAsync(ct);
        return true;
    }
}