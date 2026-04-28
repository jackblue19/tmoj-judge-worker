using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Classes.Specs;

/// <summary>
/// Lấy tất cả ClassMember đang active của một class, kèm thông tin User (để lấy DisplayName).
/// </summary>
public class ActiveClassMembersWithUserSpec : Specification<ClassMember>
{
    public ActiveClassMembersWithUserSpec(Guid classSemesterId)
    {
        Query
            .Where(m => m.ClassSemesterId == classSemesterId && m.IsActive)
            .Include(m => m.User);
    }
}
