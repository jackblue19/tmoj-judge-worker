using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.RemoveClassSemester;

public class RemoveClassSemesterCommandHandler : IRequestHandler<RemoveClassSemesterCommand>
{
    private readonly IClassRepository _repo;

    public RemoveClassSemesterCommandHandler(IClassRepository repo) => _repo = repo;

    public Task Handle(RemoveClassSemesterCommand request, CancellationToken ct) =>
        _repo.RemoveClassSemesterAsync(request.ClassId, request.ClassSemesterId, ct);
}
