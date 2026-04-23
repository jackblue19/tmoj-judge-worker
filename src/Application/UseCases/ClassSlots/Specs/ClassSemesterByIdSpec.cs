using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ClassSlots.Specs;

public class ClassSemesterByIdSpec : Specification<ClassSemester>
{
    public ClassSemesterByIdSpec(Guid classSemesterId)
    {
        Query
            .Where(cs => cs.Id == classSemesterId)
            .Include(cs => cs.Class)
            .Include(cs => cs.Subject)
            .Include(cs => cs.ClassMembers)
                .ThenInclude(cm => cm.User)
            .Include(cs => cs.ClassSlots)
                .ThenInclude(slot => slot.ClassSlotProblems)
                    .ThenInclude(csp => csp.Problem);
    }
}
