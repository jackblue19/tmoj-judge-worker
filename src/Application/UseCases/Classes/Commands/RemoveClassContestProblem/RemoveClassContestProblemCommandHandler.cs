using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.RemoveClassContestProblem;

public class RemoveClassContestProblemCommandHandler : IRequestHandler<RemoveClassContestProblemCommand>
{
    private readonly IClassRepository _repo;

    public RemoveClassContestProblemCommandHandler(IClassRepository repo) => _repo = repo;

    public Task Handle(RemoveClassContestProblemCommand request, CancellationToken ct) =>
        _repo.RemoveContestProblemAsync(
            request.ClassSemesterId, request.ContestId, request.ContestProblemId, ct);
}
