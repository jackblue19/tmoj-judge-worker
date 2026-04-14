using Application.Common.Interfaces;
using Application.UseCases.Contests.Dtos;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Common.Repositories;

public class UserRepository : IUserRepository
{
    private readonly TmojDbContext _db;

    public UserRepository(TmojDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsUserActiveAsync(Guid userId)
    {
        return await _db.Users
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId && x.Status == true);
    }

    public async Task<List<Guid>> GetLockedUsersAsync(List<Guid> userIds)
    {
        if (userIds == null || userIds.Count == 0)
            return new List<Guid>();

        return await _db.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId) && x.Status == false)
            .Select(x => x.UserId)
            .ToListAsync();
    }

    public async Task<List<User>> GetUsersByIdsAsync(List<Guid> userIds)
    {
        if (userIds == null || userIds.Count == 0)
            return new List<User>();

        return await _db.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .ToListAsync();
    }

    public async Task<List<UserDisplayDto>> GetUserDisplayByIdsAsync(List<Guid> userIds)
    {
        if (userIds == null || userIds.Count == 0)
            return new List<UserDisplayDto>();

        return await _db.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.UserId))
            .Select(x => new UserDisplayDto
            {
                UserId = x.UserId,
                DisplayName =
                    x.DisplayName ??
                    (x.FirstName + " " + x.LastName) ??
                    x.Username ??
                    x.Email
            })
            .ToListAsync();
    }

    public async Task<List<TimeConflictUserDto>> GetTimeConflictUsersAsync(
        List<Guid> userIds,
        DateTime start,
        DateTime end)
    {
        if (userIds == null || userIds.Count == 0)
            return new List<TimeConflictUserDto>();

        return await _db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.UserId))
            .Where(u =>
                u.TeamMembers.Any(tm =>
                    tm.Team.ContestTeams.Any(ct =>
                        ct.Contest.StartAt < end &&
                        ct.Contest.EndAt > start
                    )
                )
            )
            .Select(u => new TimeConflictUserDto
            {
                UserId = u.UserId,
                Name =
                    u.DisplayName ??
                    (u.FirstName + " " + u.LastName) ??
                    u.Username ??
                    u.Email
            })
            .ToListAsync();
    }
}