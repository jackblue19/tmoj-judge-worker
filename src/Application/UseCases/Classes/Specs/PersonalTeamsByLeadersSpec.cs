using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Classes.Specs;

/// <summary>
/// Lấy tất cả personal Team của một danh sách user IDs trong một query duy nhất.
/// </summary>
public class PersonalTeamsByLeadersSpec : Specification<Team>
{
    public PersonalTeamsByLeadersSpec(IEnumerable<Guid> leaderIds)
    {
        Query.Where(t => leaderIds.Contains(t.LeaderId) && t.IsPersonal);
    }
}
