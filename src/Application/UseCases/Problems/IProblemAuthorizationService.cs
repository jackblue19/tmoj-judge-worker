using Domain.Entities;

namespace Application.UseCases.Problems;

public interface IProblemAuthorizationService
{
    bool CanManage(Problem problem, Guid currentUserId, bool isAdmin);
}
