using MediatR;

namespace Application.UseCases.ClassSlots.Commands;

public record ToggleClassSlotPublishCommand(Guid ClassSemesterId, Guid SlotId) : IRequest<bool>;
