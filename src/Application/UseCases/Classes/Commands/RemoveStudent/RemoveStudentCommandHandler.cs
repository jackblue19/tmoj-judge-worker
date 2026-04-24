using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.RemoveStudent;

public class RemoveStudentCommandHandler : IRequestHandler<RemoveStudentCommand>
{
    private readonly IClassRepository _repo;

    public RemoveStudentCommandHandler(IClassRepository repo) => _repo = repo;

    public Task Handle(RemoveStudentCommand request, CancellationToken ct) =>
        _repo.RemoveStudentAsync(request.ClassSemesterId, request.StudentId, ct);
}
