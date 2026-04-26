using MediatR;

namespace Application.UseCases.Semesters.Commands.UpdateSemester;

public record UpdateSemesterCommand(
    Guid SemesterId,
    string Code,
    string Name,
    DateOnly StartAt,
    DateOnly EndAt,
    bool IsActive
) : IRequest;
