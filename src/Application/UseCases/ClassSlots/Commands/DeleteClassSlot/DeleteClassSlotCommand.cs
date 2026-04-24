using MediatR;

namespace Application.UseCases.ClassSlots.Commands;

public record DeleteClassSlotCommand(Guid ClassSemesterId, Guid SlotId) : IRequest;
