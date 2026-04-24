using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.ClassSlots.Specs;

public class SubmissionsByClassSemesterSlotsSpec : Specification<Submission>
{
    public SubmissionsByClassSemesterSlotsSpec(List<Guid> classSlotIds)
    {
        Query
            .Where(s => classSlotIds.Contains(s.ClassSlotId ?? Guid.Empty)
                       && !s.IsDeleted);
    }
}
