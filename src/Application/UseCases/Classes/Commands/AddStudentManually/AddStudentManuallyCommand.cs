using MediatR;

namespace Application.UseCases.Classes.Commands.AddStudentManually;

public record AddStudentManuallyCommand(
    Guid ClassSemesterId,
    string? RollNumber,
    string? MemberCode,
    Guid AddedByUserId
) : IRequest;
