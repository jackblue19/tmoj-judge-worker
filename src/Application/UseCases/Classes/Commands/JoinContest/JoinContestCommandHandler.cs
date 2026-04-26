using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.JoinContest;

public class JoinContestCommandHandler : IRequestHandler<JoinContestCommand>
{
    private readonly IClassRepository _repo;

    public JoinContestCommandHandler(IClassRepository repo) => _repo = repo;

    public Task Handle(JoinContestCommand request, CancellationToken ct) =>
        _repo.JoinContestAsync(request.ClassSemesterId, request.ContestId, request.UserId, ct);
}
