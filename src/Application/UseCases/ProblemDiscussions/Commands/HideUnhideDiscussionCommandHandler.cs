using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;
using Application.UseCases.Reports.Specs;

namespace Application.UseCases.ProblemDiscussions.Commands;

public class HideUnhideDiscussionCommandHandler : IRequestHandler<HideUnhideDiscussionCommand, bool>
{
    private readonly IWriteRepository<ProblemDiscussion, Guid> _writeRepo;
    private readonly IReadRepository<ProblemDiscussion, Guid> _readRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly IReadRepository<ContentReport, Guid> _reportRepo;

    public HideUnhideDiscussionCommandHandler(
        IWriteRepository<ProblemDiscussion, Guid> writeRepo,
        IReadRepository<ProblemDiscussion, Guid> readRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow,
        IReadRepository<ContentReport, Guid> reportRepo)
    {
        _writeRepo = writeRepo;
        _readRepo = readRepo;
        _currentUser = currentUser;
        _uow = uow;
        _reportRepo = reportRepo;
    }

    public async Task<bool> Handle(HideUnhideDiscussionCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException();

        var discussion = await _readRepo.GetByIdAsync(request.DiscussionId, ct)
                      ?? throw new Exception("Discussion not found");

        var isAdmin = _currentUser.IsInRole("admin") || _currentUser.IsInRole("manager");

        // Only owner or admin/manager allowed
        if (discussion.UserId != userId && !isAdmin)
            throw new UnauthorizedAccessException("You are not allowed to modify this discussion visibility");

        // Prevent unhiding if moderated (has approved report)
        if (!request.Hide && !isAdmin)
        {
            var approvedReportsCount = await _reportRepo.CountAsync(
                new ApprovedReportCountSpec(discussion.Id, "discussion"), ct);

            if (approvedReportsCount > 0)
                throw new Exception("This discussion has been hidden by moderation and cannot be unhidden.");
        }

        discussion.IsHidden = request.Hide;
        
        // Ensure UpdatedAt is safe for Postgres
        discussion.UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        _writeRepo.Update(discussion);
        await _uow.SaveChangesAsync(ct);

        return true;
    }
}
