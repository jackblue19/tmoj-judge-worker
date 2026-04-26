using Application.UseCases.Semesters.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Semesters.Commands.CreateSemester;

public class CreateSemesterCommandHandler : IRequestHandler<CreateSemesterCommand, Guid>
{
    private readonly IWriteRepository<Semester, Guid> _writeRepo;
    private readonly IReadRepository<Semester, Guid> _readRepo;
    private readonly IUnitOfWork _uow;

    public CreateSemesterCommandHandler(
        IWriteRepository<Semester, Guid> writeRepo,
        IReadRepository<Semester, Guid> readRepo,
        IUnitOfWork uow)
    {
        _writeRepo = writeRepo;
        _readRepo = readRepo;
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateSemesterCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Code and Name are required.");

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var exists = await _readRepo.AnyAsync(new SemesterByCodeSpec(normalizedCode), ct);
        if (exists)
            throw new InvalidOperationException($"Semester code '{request.Code}' already exists.");

        if (request.StartAt >= request.EndAt)
            throw new ArgumentException("StartAt must be before EndAt.");

        var hasOverlap = await _readRepo.AnyAsync(
            new SemesterOverlapSpec(request.StartAt, request.EndAt, activeOnly: true), ct);
        if (hasOverlap)
            throw new InvalidOperationException("The semester time range overlaps with an existing active semester.");

        var semester = new Semester
        {
            Code = normalizedCode,
            Name = request.Name.Trim(),
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            IsActive = true
        };

        await _writeRepo.AddAsync(semester, ct);
        await _uow.SaveChangesAsync(ct);

        return semester.SemesterId;
    }
}
