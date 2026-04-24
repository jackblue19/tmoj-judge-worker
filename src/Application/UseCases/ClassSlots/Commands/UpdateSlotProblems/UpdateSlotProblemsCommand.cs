using MediatR;

namespace Application.UseCases.ClassSlots.Commands;

public record UpdateSlotProblemsCommand(
    Guid ClassSemesterId,
    Guid SlotId,
    List<SlotProblemUpdateItem> Problems
) : IRequest<int>;

public record SlotProblemUpdateItem(
    Guid ProblemId,
    int Ordinal,
    int? Points,
    bool IsRequired
);
