using MediatR;

namespace Application.UseCases.ClassSlots.Commands;

public record SetClassSlotDueDateCommand(
    Guid ClassSemesterId,
    Guid SlotId,
    DateTime DueAt,
    DateTime? CloseAt
) : IRequest;
