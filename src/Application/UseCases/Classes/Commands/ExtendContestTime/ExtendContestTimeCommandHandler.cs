using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.ExtendContestTime;

public class ExtendContestTimeCommandHandler : IRequestHandler<ExtendContestTimeCommand>
{
    private readonly IClassRepository _repo;

    public ExtendContestTimeCommandHandler(IClassRepository repo) => _repo = repo;

    public Task Handle(ExtendContestTimeCommand request, CancellationToken ct) =>
        _repo.ExtendContestTimeAsync(request.ClassSemesterId, request.ContestId, request.NewEndAt, ct);
}
