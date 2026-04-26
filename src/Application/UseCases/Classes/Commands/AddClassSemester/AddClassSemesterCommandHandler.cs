using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.AddClassSemester;

public class AddClassSemesterCommandHandler : IRequestHandler<AddClassSemesterCommand, Guid>
{
    private readonly IClassRepository _repo;

    public AddClassSemesterCommandHandler(IClassRepository repo) => _repo = repo;

    public Task<Guid> Handle(AddClassSemesterCommand request, CancellationToken ct) =>
        _repo.AddClassSemesterAsync(request.ClassId, request.SemesterId, request.SubjectId, request.TeacherId, ct);
}
