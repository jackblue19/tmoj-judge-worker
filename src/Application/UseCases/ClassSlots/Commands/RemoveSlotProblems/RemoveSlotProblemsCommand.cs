using MediatR;

namespace Application.UseCases.ClassSlots.Commands;

public record RemoveSlotProblemsCommand(
    Guid ClassSemesterId,
    Guid SlotId,
    List<Guid> ProblemIds
) : IRequest<int>;
