using MediatR;

namespace Application.UseCases.Classes.Commands.DeleteClass;

public record DeleteClassCommand(Guid ClassId) : IRequest;
