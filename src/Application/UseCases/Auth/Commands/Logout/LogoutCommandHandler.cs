using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IAuthRepository _repo;

    public LogoutCommandHandler(IAuthRepository repo) => _repo = repo;

    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        var sessions = await _repo.GetSessionsWithTokensAsync(request.UserId, ct);
        _repo.RemoveSessions(sessions);
        await _repo.SaveAsync(ct);
    }
}
