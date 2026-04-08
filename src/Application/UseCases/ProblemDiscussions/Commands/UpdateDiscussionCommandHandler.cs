using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.ProblemDiscussions.Commands;

public class UpdateDiscussionCommandHandler
    : IRequestHandler<UpdateDiscussionCommand, bool>
{
    private readonly IWriteRepository<ProblemDiscussion, Guid> _writeRepo;
    private readonly IReadRepository<ProblemDiscussion, Guid> _readRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public UpdateDiscussionCommandHandler(
        IWriteRepository<ProblemDiscussion, Guid> writeRepo,
        IReadRepository<ProblemDiscussion, Guid> readRepo,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _writeRepo = writeRepo;
        _readRepo = readRepo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<bool> Handle(UpdateDiscussionCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            throw new UnauthorizedAccessException("User not authenticated");

        var discussion = await _readRepo.GetByIdAsync(request.Id, ct);
        if (discussion == null)
            throw new Exception("Discussion not found");

        // ===============================
        // 🚨 BONUS 1: LOCK CHECK
        // ===============================
        if (discussion.IsLocked == true)
            throw new Exception("Discussion is locked and cannot be edited");

        // ===============================
        // 🚨 BONUS 2: SOFT DELETE CHECK (nếu có)
        // ===============================
        // 👉 chỉ bật nếu entity có IsDeleted
        // if (discussion.IsDeleted == true)
        //     throw new Exception("Discussion has been deleted");

        // ===============================
        // PERMISSION
        // ===============================
        var isOwner = discussion.UserId == userId.Value;
        var isAdmin = _currentUser.IsInRole("admin") || _currentUser.IsInRole("manager");

        if (!isOwner && !isAdmin)
            throw new UnauthorizedAccessException("You cannot edit this discussion");

        // ===============================
        // UPDATE
        // ===============================
        discussion.Title = request.Title;
        discussion.Content = request.Content;

        // optional: track update time
        // discussion.UpdatedAt = DateTime.UtcNow;

        _writeRepo.Update(discussion);
        await _uow.SaveChangesAsync(ct);

        return true;
    }
}