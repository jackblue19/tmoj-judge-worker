using MediatR;

namespace Application.UseCases.ClassSlots.Commands;

public record AddSlotProblemsCommand(
    Guid ClassSemesterId,
    Guid SlotId,
    List<SlotProblemItem> Problems
) : IRequest<int>;

public record SlotProblemItem(
    Guid ProblemId,
    int Ordinal,
    int? Points,
    bool IsRequired
);
