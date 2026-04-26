using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.CancelInviteCode;

public class CancelInviteCodeCommandHandler : IRequestHandler<CancelInviteCodeCommand>
{
    private readonly IClassRepository _repo;

    public CancelInviteCodeCommandHandler(IClassRepository repo) => _repo = repo;

    public Task Handle(CancelInviteCodeCommand request, CancellationToken ct) =>
        _repo.CancelInviteCodeAsync(request.ClassSemesterId, ct);
}
