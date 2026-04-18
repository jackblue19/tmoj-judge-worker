using Ardalis.Specification;
using Domain.Entities;

namespace Application.UseCases.Contests.Specs;

public class ContestByInviteCodeSpec
    : Specification<Contest>, ISingleResultSpecification<Contest>
{
    public ContestByInviteCodeSpec(string inviteCode)
    {
        Query.Where(c => c.InviteCode == inviteCode && c.IsActive);
    }
}
