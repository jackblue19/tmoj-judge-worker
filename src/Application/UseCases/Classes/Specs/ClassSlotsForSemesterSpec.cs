using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Classes.Specs;

/// <summary>
/// Lấy tất cả ClassSlot của một ClassSemester để tính SlotNo tiếp theo.
/// </summary>
public class ClassSlotsForSemesterSpec : Specification<ClassSlot>
{
    public ClassSlotsForSemesterSpec(Guid classSemesterId)
    {
        Query.Where(s => s.ClassSemesterId == classSemesterId);
    }
}
