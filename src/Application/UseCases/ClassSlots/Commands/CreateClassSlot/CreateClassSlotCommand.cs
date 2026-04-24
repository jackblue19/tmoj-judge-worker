using MediatR;

namespace Application.UseCases.ClassSlots.Commands;

public record CreateClassSlotCommand(
    Guid ClassSemesterId,
    int SlotNo,
    string Title,
    string? Description,
    string? Rules,
    DateTime? OpenAt,
    DateTime? DueAt,
    DateTime? CloseAt,
    string Mode,
    List<CreateSlotProblemItem>? Problems
) : IRequest<Guid>;

public record CreateSlotProblemItem(
    Guid ProblemId,
    int? Ordinal,
    int? Points,
    bool IsRequired
);
