using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.UpdateClassContestProblem;

public class UpdateClassContestProblemCommandHandler : IRequestHandler<UpdateClassContestProblemCommand>
{
    private readonly IClassRepository _repo;

    public UpdateClassContestProblemCommandHandler(IClassRepository repo) => _repo = repo;

    public Task Handle(UpdateClassContestProblemCommand request, CancellationToken ct) =>
        _repo.UpdateContestProblemAsync(
            request.ClassSemesterId, request.ContestId, request.ContestProblemId,
            request.Alias, request.Ordinal, request.Points, request.MaxScore,
            request.TimeLimitMs, request.MemoryLimitKb, ct);
}
