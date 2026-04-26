using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ClassSlots.Specs;

public sealed class ClassSlotByClassSemesterAndSlotNoSpec : Specification<ClassSlot>
{
    public ClassSlotByClassSemesterAndSlotNoSpec(Guid classSemesterId, int slotNo)
    {
        Query.Where(s => s.ClassSemesterId == classSemesterId && s.SlotNo == slotNo);
    }
}
