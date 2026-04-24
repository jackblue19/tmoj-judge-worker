using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.UpdateClass;

public class UpdateClassCommandHandler : IRequestHandler<UpdateClassCommand>
{
    private readonly IClassRepository _repo;

    public UpdateClassCommandHandler(IClassRepository repo) => _repo = repo;

    public Task Handle(UpdateClassCommand request, CancellationToken ct) =>
        _repo.UpdateClassAsync(request.ClassId, request.IsActive, ct);
}
