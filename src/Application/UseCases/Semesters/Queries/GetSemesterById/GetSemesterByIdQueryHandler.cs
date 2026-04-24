using Application.UseCases.Semesters.Dtos;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Semesters.Queries.GetSemesterById;

public class GetSemesterByIdQueryHandler : IRequestHandler<GetSemesterByIdQuery, SemesterDto>
{
    private readonly IReadRepository<Semester, Guid> _readRepo;

    public GetSemesterByIdQueryHandler(IReadRepository<Semester, Guid> readRepo)
    {
        _readRepo = readRepo;
    }

    public async Task<SemesterDto> Handle(GetSemesterByIdQuery request, CancellationToken ct)
    {
        var semester = await _readRepo.GetByIdAsync(request.SemesterId, ct)
            ?? throw new KeyNotFoundException("Semester not found.");

        return new SemesterDto(
            semester.SemesterId,
            semester.Code,
            semester.Name,
            semester.StartAt,
            semester.EndAt,
            semester.IsActive,
            semester.CreatedAt);
    }
}
