using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.CreateClass;

public class CreateClassCommandHandler : IRequestHandler<CreateClassCommand, (Guid ClassId, Guid InstanceId)>
{
    private readonly IClassRepository _repo;

    public CreateClassCommandHandler(IClassRepository repo) => _repo = repo;

    public Task<(Guid ClassId, Guid InstanceId)> Handle(CreateClassCommand request, CancellationToken ct) =>
        _repo.CreateClassAsync(request.Code, request.SubjectId, request.SemesterId, request.TeacherId, ct);
}
