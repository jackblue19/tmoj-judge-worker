using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Users.Commands.SetAccountStatus;

public record SetAccountStatusCommand(Guid UserId, bool Status) : IRequest;

public class SetAccountStatusCommandHandler : IRequestHandler<SetAccountStatusCommand>
{
    private readonly IUserManagementRepository _repo;

    public SetAccountStatusCommandHandler(IUserManagementRepository repo) => _repo = repo;

    public async Task Handle(SetAccountStatusCommand req, CancellationToken ct)
    {
        var user = await _repo.FindUserAsync(req.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        user.Status = req.Status;
        await _repo.SaveAsync(ct);
    }
}
