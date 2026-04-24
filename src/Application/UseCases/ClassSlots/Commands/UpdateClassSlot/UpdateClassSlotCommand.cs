using MediatR;

namespace Application.UseCases.ClassSlots.Commands;

public record UpdateClassSlotCommand(
    Guid ClassSemesterId,
    Guid SlotId,
    string? Title,
    string? Description,
    string? Rules,
    DateTime? OpenAt,
    DateTime? DueAt,
    DateTime? CloseAt,
    bool? IsPublished
) : IRequest;
