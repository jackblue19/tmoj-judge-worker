using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Semesters.Commands.DeleteSemester;

public class DeleteSemesterCommandHandler : IRequestHandler<DeleteSemesterCommand>
{
    private readonly IReadRepository<Semester, Guid> _readRepo;
    private readonly IWriteRepository<Semester, Guid> _writeRepo;
    private readonly IUnitOfWork _uow;
    private readonly ISemesterRepository _semesterRepo;

    public DeleteSemesterCommandHandler(
        IReadRepository<Semester, Guid> readRepo,
        IWriteRepository<Semester, Guid> writeRepo,
        IUnitOfWork uow,
        ISemesterRepository semesterRepo)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _uow = uow;
        _semesterRepo = semesterRepo;
    }

    public async Task Handle(DeleteSemesterCommand request, CancellationToken ct)
    {
        var semester = await _readRepo.GetByIdAsync(request.SemesterId, ct)
            ?? throw new KeyNotFoundException("Semester not found.");

        // Check if any active classes are linked to this semester
        var hasActiveClasses = await _semesterRepo.HasActiveClassesAsync(request.SemesterId, ct);
        if (hasActiveClasses)
            throw new InvalidOperationException("Cannot delete semester because it is being used by active classes.");

        semester.IsActive = false;
        _writeRepo.Update(semester);
        await _uow.SaveChangesAsync(ct);
    }
}
