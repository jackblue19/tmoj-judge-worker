using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Classes.Commands.UpdateClassSemester;

public class UpdateClassSemesterCommandHandler : IRequestHandler<UpdateClassSemesterCommand>
{
    private readonly IClassRepository _repo;

    public UpdateClassSemesterCommandHandler(IClassRepository repo) => _repo = repo;

    public Task Handle(UpdateClassSemesterCommand request, CancellationToken ct) =>
        _repo.UpdateClassSemesterAsync(request.ClassId, request.ClassSemesterId, request.NewClassId, request.SemesterId, request.SubjectId, request.TeacherId, ct);
}
