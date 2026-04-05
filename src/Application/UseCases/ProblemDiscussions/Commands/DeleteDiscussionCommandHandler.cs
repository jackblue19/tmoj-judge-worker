using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.ProblemDiscussions.Commands;

public class DeleteDiscussionCommandHandler
    : IRequestHandler<DeleteDiscussionCommand, bool>
{
    private readonly IWriteRepository<ProblemDiscussion, Guid> _writeRepo;
    private readonly IReadRepository<ProblemDiscussion, Guid> _readRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DeleteDiscussionCommandHandler> _logger;

    public DeleteDiscussionCommandHandler(
        IWriteRepository<ProblemDiscussion, Guid> writeRepo,
        IReadRepository<ProblemDiscussion, Guid> readRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        ILogger<DeleteDiscussionCommandHandler> logger)
    {
        _writeRepo = writeRepo;
        _readRepo = readRepo;
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteDiscussionCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (userId is null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var discussion = await _readRepo.GetByIdAsync(request.Id, ct);
        if (discussion is null)
            throw new Exception("Discussion not found.");

        var isOwner = discussion.UserId == userId.Value;
        var isAdmin = _currentUser.IsInRole("admin") || _currentUser.IsInRole("manager");

        if (!isOwner && !isAdmin)
            throw new UnauthorizedAccessException("You are not allowed to delete this discussion.");

        try
        {
            _writeRepo.Remove(discussion);

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Discussion deleted: {Id}", request.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed: {Id}", request.Id);
            throw;
        }

        return true;
    }
}
