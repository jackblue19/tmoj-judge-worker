using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.JoinClassByCode;

public class JoinClassByCodeCommandHandler : IRequestHandler<JoinClassByCodeCommand, (Guid ClassId, Guid ClassSemesterId)>
{
    private readonly IClassRepository _repo;

    public JoinClassByCodeCommandHandler(IClassRepository repo) => _repo = repo;

    public Task<(Guid ClassId, Guid ClassSemesterId)> Handle(JoinClassByCodeCommand request, CancellationToken ct) =>
        _repo.JoinByInviteCodeAsync(request.Code, request.UserId, ct);
}
