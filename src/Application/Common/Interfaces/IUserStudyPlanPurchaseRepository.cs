using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IUserStudyPlanPurchaseRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid studyPlanId);

    Task AddAsync(UserStudyPlanPurchase entity);

    Task SaveChangesAsync();
}