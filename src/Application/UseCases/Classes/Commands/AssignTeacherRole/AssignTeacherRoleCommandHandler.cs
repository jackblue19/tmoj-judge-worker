using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.AssignTeacherRole;

public class AssignTeacherRoleCommandHandler : IRequestHandler<AssignTeacherRoleCommand>
{
    private readonly IClassRepository _repo;

    public AssignTeacherRoleCommandHandler(IClassRepository repo) => _repo = repo;

    public Task Handle(AssignTeacherRoleCommand request, CancellationToken ct) =>
        _repo.AssignTeacherRoleAsync(request.UserId, ct);
}
