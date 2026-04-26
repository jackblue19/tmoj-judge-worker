using Application.UseCases.Semesters.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Semesters.Commands.UpdateSemester;

public class UpdateSemesterCommandHandler : IRequestHandler<UpdateSemesterCommand>
{
    private readonly IReadRepository<Semester, Guid> _readRepo;
    private readonly IWriteRepository<Semester, Guid> _writeRepo;
    private readonly IUnitOfWork _uow;

    public UpdateSemesterCommandHandler(
        IReadRepository<Semester, Guid> readRepo,
        IWriteRepository<Semester, Guid> writeRepo,
        IUnitOfWork uow)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _uow = uow;
    }

    public async Task Handle(UpdateSemesterCommand request, CancellationToken ct)
    {
        var semester = await _readRepo.GetByIdAsync(request.SemesterId, ct)
            ?? throw new KeyNotFoundException("Semester not found.");

        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _readRepo.AnyAsync(new SemesterByCodeSpec(normalizedCode), ct);
        if (codeExists && semester.Code != normalizedCode)
            throw new InvalidOperationException($"Semester code '{request.Code}' already exists.");

        if (request.StartAt >= request.EndAt)
            throw new ArgumentException("StartAt must be before EndAt.");

        var hasOverlap = await _readRepo.AnyAsync(
            new SemesterOverlapExceptIdSpec(request.SemesterId, request.StartAt, request.EndAt), ct);
        if (hasOverlap)
            throw new InvalidOperationException("The semester time range overlaps with an existing active semester.");

        semester.Code = normalizedCode;
        semester.Name = request.Name.Trim();
        semester.StartAt = request.StartAt;
        semester.EndAt = request.EndAt;
        semester.IsActive = request.IsActive;

        _writeRepo.Update(semester);
        await _uow.SaveChangesAsync(ct);
    }
}
