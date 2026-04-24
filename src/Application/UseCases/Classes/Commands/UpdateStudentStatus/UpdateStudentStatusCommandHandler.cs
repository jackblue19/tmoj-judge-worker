using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.UpdateStudentStatus;

public class UpdateStudentStatusCommandHandler : IRequestHandler<UpdateStudentStatusCommand>
{
    private readonly IClassRepository _repo;

    public UpdateStudentStatusCommandHandler(IClassRepository repo) => _repo = repo;

    public Task Handle(UpdateStudentStatusCommand request, CancellationToken ct) =>
        _repo.UpdateStudentStatusAsync(request.ClassSemesterId, request.StudentId, request.IsActive, ct);
}
