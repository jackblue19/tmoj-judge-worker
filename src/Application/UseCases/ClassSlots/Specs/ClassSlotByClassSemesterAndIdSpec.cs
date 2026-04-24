using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ClassSlots.Specs;

public sealed class ClassSlotByClassSemesterAndIdSpec : Specification<ClassSlot>
{
    public ClassSlotByClassSemesterAndIdSpec(Guid classSemesterId, Guid slotId)
    {
        Query.Where(s => s.ClassSemesterId == classSemesterId && s.Id == slotId);
    }
}
