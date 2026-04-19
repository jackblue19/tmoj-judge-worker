using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface IStudyPlanRepository
{
    // =========================
    // PLAN
    // =========================
    Task<List<StudyPlan>> GetAllAsync();
    Task<Guid> CreateAsync(StudyPlan entity);
    Task<StudyPlan?> GetByIdAsync(Guid id);
    Task<List<StudyPlan>> GetByCreatorAsync(Guid creatorId);

    // =========================
    // ITEMS
    // =========================
    Task AddItemAsync(StudyPlanItem entity);
    Task<List<StudyPlanItem>> GetItemsByPlanIdAsync(Guid studyPlanId);

    // =========================
    // ITEM PROGRESS (ONLY SOURCE OF TRUTH)
    // =========================
    Task<UserStudyItemProgress?> GetItemProgressAsync(Guid userId, Guid studyPlanItemId);
    Task<List<UserStudyItemProgress>> GetItemProgressByPlanAsync(Guid userId, Guid studyPlanId);
    Task<List<UserStudyItemProgress>> GetAllItemProgressByUserAsync(Guid userId);
    Task<StudyPlanItem?> GetItemByIdAsync(Guid itemId);
    Task<List<UserStudyItemProgress>> GetAllProgressByPlanAsync(Guid studyPlanId);
    Task CreateItemProgressAsync(UserStudyItemProgress entity);

    // =========================
    // LOGIC
    // =========================
    Task<bool> IsUserEnrolledAsync(Guid userId, Guid studyPlanId);
    Task<bool> IsStudyPlanCompletedAsync(Guid userId, Guid studyPlanId);
    Task DeleteItemProgressRangeAsync(List<UserStudyItemProgress> entities);

    // =========================
    // SAVE
    // =========================
    Task SaveChangesAsync();
}