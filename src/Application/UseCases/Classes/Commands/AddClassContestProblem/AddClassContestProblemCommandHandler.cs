using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.AddClassContestProblem;

public class AddClassContestProblemCommandHandler : IRequestHandler<AddClassContestProblemCommand, Guid>
{
    private readonly IClassRepository _repo;

    public AddClassContestProblemCommandHandler(IClassRepository repo) => _repo = repo;

    public Task<Guid> Handle(AddClassContestProblemCommand request, CancellationToken ct) =>
        _repo.AddContestProblemAsync(
            request.ClassSemesterId, request.ContestId, request.CreatedBy,
            request.ProblemId, request.Alias, request.Ordinal,
            request.Points, request.MaxScore, request.TimeLimitMs, request.MemoryLimitKb, ct);
}
