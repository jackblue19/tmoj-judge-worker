using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ClassSlots.Specs;

public class SubmissionsByClassSlotIdSpec : Specification<Submission>
{
    public SubmissionsByClassSlotIdSpec(Guid classSlotId)
    {
        Query
            .Where(s => s.ClassSlotId == classSlotId && !s.IsDeleted);
    }
}
