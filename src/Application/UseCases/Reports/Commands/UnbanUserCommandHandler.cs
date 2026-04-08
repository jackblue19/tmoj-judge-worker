using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Interfaces;

namespace Application.UseCases.Users.Commands;

public class UnbanUserCommandHandler : IRequestHandler<UnbanUserCommand, Unit>
{
    private readonly IReadRepository<User, Guid> _readRepo;
    private readonly IWriteRepository<User, Guid> _writeRepo;
    private readonly IWriteRepository<ModerationAction, Guid> _actionRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public UnbanUserCommandHandler(
        IReadRepository<User, Guid> readRepo,
        IWriteRepository<User, Guid> writeRepo,
        IWriteRepository<ModerationAction, Guid> actionRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _actionRepo = actionRepo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UnbanUserCommand request, CancellationToken ct)
    {
        var adminId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException();

        var user = await _readRepo.GetByIdAsync(request.UserId, ct)
            ?? throw new Exception("User not found");

        if (user.Status == true)
            throw new Exception("User is not banned");

        // 🔥 FIX DateTime PostgreSQL
        var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        user.Status = true;
        _writeRepo.Update(user);

        // 🔥 LOG ACTION
        await _actionRepo.AddAsync(new ModerationAction
        {
            Id = Guid.NewGuid(),
            ReportId = Guid.Empty, // unban không gắn với report cụ thể
            AdminId = adminId,
            ActionType = "unban_user",
            Note = "Admin manually unbanned user",
            CreatedAt = now
        }, ct);

        await _uow.SaveChangesAsync(ct);

        return Unit.Value;
    }
}