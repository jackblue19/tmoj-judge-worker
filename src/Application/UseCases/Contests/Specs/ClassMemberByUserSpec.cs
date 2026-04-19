using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Contests.Specs;

public class ClassMemberByUserSpec : Specification<ClassMember>
{
    public ClassMemberByUserSpec(Guid classSemesterId, Guid userId)
    {
        Query.Where(m =>
            m.ClassSemesterId == classSemesterId &&
            m.UserId == userId &&
            m.IsActive);
    }
}
