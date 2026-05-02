using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.UnfreezeClassContest;

public class UnfreezeClassContestCommandHandler : IRequestHandler<UnfreezeClassContestCommand>
{
    private readonly IClassRepository _repo;

    public UnfreezeClassContestCommandHandler(IClassRepository repo) => _repo = repo;

    public Task Handle(UnfreezeClassContestCommand request, CancellationToken ct) =>
        _repo.UnfreezeContestAsync(request.ClassSemesterId, request.ContestId, request.UserId, ct);
}
