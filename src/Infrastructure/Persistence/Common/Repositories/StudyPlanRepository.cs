using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Common.Repositories;

public class StudyPlanRepository : IStudyPlanRepository
{
    private readonly TmojDbContext _db;

    public StudyPlanRepository(TmojDbContext db)
    {
        _db = db;
    }

    // =========================
    // PLAN
    // =========================
    public async Task<Guid> CreateAsync(StudyPlan entity)
    {
        await _db.Set<StudyPlan>().AddAsync(entity);
        return entity.Id;
    }

    public async Task<StudyPlan?> GetByIdAsync(Guid id)
    {
        return await _db.Set<StudyPlan>()
            .Include(x => x.StudyPlanItems)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<List<StudyPlan>> GetByCreatorAsync(Guid creatorId)
    {
        return await _db.Set<StudyPlan>()
            .Where(x => x.CreatorId == creatorId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
    public async Task<Dictionary<Guid, Guid>> GetItemPlanMappingAsync(List<Guid> itemIds)
    {
        if (itemIds == null || itemIds.Count == 0)
            return new Dictionary<Guid, Guid>();

        return await _db.Set<StudyPlanItem>()
            .Where(x => itemIds.Contains(x.Id))
            .Select(x => new { x.Id, x.StudyPlanId })
            .ToDictionaryAsync(x => x.Id, x => x.StudyPlanId);
    }
    public async Task<Dictionary<Guid, string>> GetPlanTitlesAsync(List<Guid> planIds)
    {
        if (planIds == null || planIds.Count == 0)
            return new Dictionary<Guid, string>();

        return await _db.Set<StudyPlan>()
            .Where(x => planIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Title })
            .ToDictionaryAsync(x => x.Id, x => x.Title);
    }

    public async Task<Dictionary<Guid, bool>> GetCompletedPlansAsync(Guid userId, List<Guid> planIds)
    {
        if (planIds == null || planIds.Count == 0)
            return new Dictionary<Guid, bool>();

        // total items per plan
        var totalItems = await _db.Set<StudyPlanItem>()
            .Where(x => planIds.Contains(x.StudyPlanId))
            .GroupBy(x => x.StudyPlanId)
            .Select(g => new { PlanId = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.PlanId, x => x.Total);

        // completed items per plan
        var completedItems = await (
            from p in _db.Set<UserStudyItemProgress>()
            join i in _db.Set<StudyPlanItem>()
                on p.StudyPlanItemId equals i.Id
            where p.UserId == userId
                  && planIds.Contains(i.StudyPlanId)
                  && p.IsCompleted == true
            group p by i.StudyPlanId into g
            select new
            {
                PlanId = g.Key,
                Completed = g.Select(x => x.StudyPlanItemId).Distinct().Count()
            }
        ).ToDictionaryAsync(x => x.PlanId, x => x.Completed);

        var result = new Dictionary<Guid, bool>();

        foreach (var planId in planIds)
        {
            var total = totalItems.GetValueOrDefault(planId);
            var completed = completedItems.GetValueOrDefault(planId);

            result[planId] = total > 0 && completed == total;
        }

        return result;
    }

    // =========================
    // ITEMS
    // =========================
    public async Task AddItemAsync(StudyPlanItem entity)
    {
        await _db.Set<StudyPlanItem>().AddAsync(entity);
    }

    public async Task<List<StudyPlanItem>> GetItemsByPlanIdAsync(Guid studyPlanId)
    {
        return await _db.Set<StudyPlanItem>()
            .Where(x => x.StudyPlanId == studyPlanId)
            .OrderBy(x => x.OrderIndex)
            .ToListAsync();
    }
    public async Task<int> GetItemCountAsync(Guid planId)
    {
        return await _db.Set<StudyPlanItem>()
            .CountAsync(x => x.StudyPlanId == planId);
    }

    // =========================
    // ITEM PROGRESS
    // =========================
    public async Task<UserStudyItemProgress?> GetItemProgressAsync(Guid userId, Guid studyPlanItemId)
    {
        return await _db.Set<UserStudyItemProgress>()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.StudyPlanItemId == studyPlanItemId);
    }

    public async Task<List<UserStudyItemProgress>> GetItemProgressByPlanAsync(Guid userId, Guid studyPlanId)
    {
        return await (
            from p in _db.Set<UserStudyItemProgress>()
            join i in _db.Set<StudyPlanItem>()
                on p.StudyPlanItemId equals i.Id
            where p.UserId == userId
                  && i.StudyPlanId == studyPlanId
            select p
        ).ToListAsync();
    }

    public async Task CreateItemProgressAsync(UserStudyItemProgress entity)
    {
        await _db.Set<UserStudyItemProgress>().AddAsync(entity);
    }

    // =========================
    // ENROLL CHECK
    // =========================
    public async Task<bool> IsUserEnrolledAsync(Guid userId, Guid studyPlanId)
    {
        return await (
            from p in _db.Set<UserStudyItemProgress>()
            join i in _db.Set<StudyPlanItem>()
                on p.StudyPlanItemId equals i.Id
            where p.UserId == userId
                  && i.StudyPlanId == studyPlanId
            select p.Id
        ).AnyAsync();
    }

    // =========================
    // COMPLETION CHECK (OPTIMIZED - NO LIST IN MEMORY)
    // =========================
    public async Task<bool> IsStudyPlanCompletedAsync(Guid userId, Guid studyPlanId)
    {
        var totalItems = await _db.Set<StudyPlanItem>()
            .CountAsync(x => x.StudyPlanId == studyPlanId);

        if (totalItems == 0)
            return false;

        var completedItems = await (
            from p in _db.Set<UserStudyItemProgress>()
            join i in _db.Set<StudyPlanItem>()
                on p.StudyPlanItemId equals i.Id
            where p.UserId == userId
                  && i.StudyPlanId == studyPlanId
                  && p.IsCompleted == true
            select p.StudyPlanItemId
        )
        .Distinct()
        .CountAsync();

        return completedItems == totalItems;
    }
    // =========================
    // SAVE
    // =========================
    public async Task SaveChangesAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var entries = _db.ChangeTracker.Entries()
                .Select(e => new
                {
                    Entity = e.Entity.GetType().Name,
                    State = e.State.ToString()
                });

            var debugInfo = string.Join(", ", entries.Select(x => $"{x.Entity}:{x.State}"));

            var inner = ex.InnerException?.Message;

            throw new Exception(
                $"DB ERROR: {inner ?? ex.Message} | ENTITIES: {debugInfo}",
                ex
            );
        }
    }

    // =========================
    // GET ALL ITEM PROGRESS BY USER (FOR DASHBOARD, ETC.)
    // =========================

    public async Task<List<UserStudyItemProgress>> GetAllItemProgressByUserAsync(Guid userId)
    {
        return await _db.Set<UserStudyItemProgress>()
            .Include(x => x.StudyPlanItem)
                .ThenInclude(i => i.StudyPlan)
            .Where(x => x.UserId == userId)
            .ToListAsync();
    }
    // =========================
    // DELETE ITEM PROGRESS RANGE (FOR UNENROLL OR RESET PROGRESS)
    // =========================

    public Task DeleteItemProgressRangeAsync(List<UserStudyItemProgress> entities)
    {
        _db.Set<UserStudyItemProgress>().RemoveRange(entities);
        return Task.CompletedTask;
    }
    // =========================
    // GET ITEM BY ID (FOR PROGRESS UPDATE, ETC.)
    // =========================
    public async Task<StudyPlanItem?> GetItemByIdAsync(Guid itemId)
    {
        return await _db.Set<StudyPlanItem>()
            .FirstOrDefaultAsync(x => x.Id == itemId);
    }

    // =========================
    // GET ALL PROGRESS BY PLAN (FOR ANALYTICS, ETC.)
    // =========================
    public async Task<List<UserStudyItemProgress>> GetAllProgressByPlanAsync(Guid studyPlanId)
    {
        var itemIds = await _db.Set<StudyPlanItem>()
            .Where(x => x.StudyPlanId == studyPlanId)
            .Select(x => x.Id)
            .ToListAsync();

        return await _db.Set<UserStudyItemProgress>()
            .Where(x => itemIds.Contains(x.StudyPlanItemId))
            .ToListAsync();
    }

    public async Task<List<StudyPlan>> GetAllAsync()
    {
        return await _db.Set<StudyPlan>()
            .Include(x => x.StudyPlanItems)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> HasUserPurchasedPlanAsync(Guid userId, Guid studyPlanId)
    {
        return await _db.Set<UserStudyPlanPurchase>()
            .AnyAsync(x =>
                x.UserId == userId &&
                x.StudyPlanId == studyPlanId
            );
    }
}