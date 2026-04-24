using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.AddStudentManually;

public class AddStudentManuallyCommandHandler : IRequestHandler<AddStudentManuallyCommand>
{
    private readonly IClassRepository _repo;

    public AddStudentManuallyCommandHandler(IClassRepository repo) => _repo = repo;

    public Task Handle(AddStudentManuallyCommand request, CancellationToken ct) =>
        _repo.AddStudentManuallyAsync(request.ClassSemesterId, request.RollNumber, request.MemberCode, ct);
}
