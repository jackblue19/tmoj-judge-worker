using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ClassSlots.Specs;

public sealed class SlotProblemsBySlotAndProblemIdsSpec : Specification<ClassSlotProblem>
{
    public SlotProblemsBySlotAndProblemIdsSpec(Guid slotId, List<Guid> problemIds)
    {
        Query.Where(sp => sp.SlotId == slotId && problemIds.Contains(sp.ProblemId));
    }
}
