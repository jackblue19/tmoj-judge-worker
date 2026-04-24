using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Common.Repositories;

public class UserStudyPlanPurchaseRepository : IUserStudyPlanPurchaseRepository
{
    private readonly TmojDbContext _db;

    public UserStudyPlanPurchaseRepository(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ExistsAsync(Guid userId, Guid studyPlanId)
    {
        return await _db.Set<UserStudyPlanPurchase>()
            .AnyAsync(x => x.UserId == userId && x.StudyPlanId == studyPlanId);
    }

    public async Task AddAsync(UserStudyPlanPurchase entity)
    {
        await _db.Set<UserStudyPlanPurchase>().AddAsync(entity);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}