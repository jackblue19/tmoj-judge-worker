using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Users.Commands.DeleteUser;

public record DeleteUserCommand(Guid UserId) : IRequest;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IUserManagementRepository _repo;

    public DeleteUserCommandHandler(IUserManagementRepository repo) => _repo = repo;

    public async Task Handle(DeleteUserCommand req, CancellationToken ct)
    {
        var user = await _repo.FindUserAsync(req.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        _repo.RemoveUser(user);
        await _repo.SaveAsync(ct);
    }
}
