using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ClassSlots.Specs;

public sealed class ClassSlotByClassSemesterAndIdWithProblemsSpec : Specification<ClassSlot>
{
    public ClassSlotByClassSemesterAndIdWithProblemsSpec(Guid classSemesterId, Guid slotId)
    {
        Query
            .Where(s => s.ClassSemesterId == classSemesterId && s.Id == slotId)
            .Include(s => s.ClassSlotProblems);
    }
}
