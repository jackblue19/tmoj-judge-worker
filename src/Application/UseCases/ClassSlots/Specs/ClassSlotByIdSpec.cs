using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ClassSlots.Specs;

public class ClassSlotByIdSpec : Specification<ClassSlot>
{
    public ClassSlotByIdSpec(Guid classSlotId)
    {
        Query
            .Where(cs => cs.Id == classSlotId)
            .Include(cs => cs.ClassSemester)
                .ThenInclude(csem => csem.ClassMembers)
                    .ThenInclude(cm => cm.User)
            .Include(cs => cs.ClassSlotProblems)
                .ThenInclude(csp => csp.Problem);
    }
}
