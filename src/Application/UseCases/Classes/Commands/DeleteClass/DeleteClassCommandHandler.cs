using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.DeleteClass;

public class DeleteClassCommandHandler : IRequestHandler<DeleteClassCommand>
{
    private readonly IClassRepository _repo;

    public DeleteClassCommandHandler(IClassRepository repo) => _repo = repo;

    public Task Handle(DeleteClassCommand request, CancellationToken ct) =>
        _repo.DeleteClassAsync(request.ClassId, ct);
}
