using MediatR;

namespace Application.UseCases.Classes.Commands.UpdateClass;

public record UpdateClassCommand(Guid ClassId, bool? IsActive) : IRequest;
