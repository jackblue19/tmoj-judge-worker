namespace Application.UseCases.Semesters.Dtos;

public record SemesterDto(
    Guid SemesterId,
    string Code,
    string Name,
    DateOnly StartAt,
    DateOnly EndAt,
    bool IsActive,
    DateTime CreatedAt
);
