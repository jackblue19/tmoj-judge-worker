using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.FreezeClassContest;

public class FreezeClassContestCommandHandler : IRequestHandler<FreezeClassContestCommand>
{
    private readonly IClassRepository _repo;

    public FreezeClassContestCommandHandler(IClassRepository repo) => _repo = repo;

    public Task Handle(FreezeClassContestCommand request, CancellationToken ct) =>
        _repo.FreezeContestAsync(request.ClassSemesterId, request.ContestId, request.UserId, ct);
}
