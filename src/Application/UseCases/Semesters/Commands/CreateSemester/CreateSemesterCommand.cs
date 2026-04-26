using MediatR;

namespace Application.UseCases.Semesters.Commands.CreateSemester;

public record CreateSemesterCommand(
    string Code,
    string Name,
    DateOnly StartAt,
    DateOnly EndAt
) : IRequest<Guid>;
