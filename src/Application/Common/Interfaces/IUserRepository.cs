using Domain.Entities;
using Application.UseCases.Contests.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface IUserRepository
{
    Task<bool> IsUserActiveAsync(Guid userId);

    Task<List<Guid>> GetLockedUsersAsync(List<Guid> userIds);

    Task<List<User>> GetUsersByIdsAsync(List<Guid> userIds);

    Task<List<UserDisplayDto>> GetUserDisplayByIdsAsync(List<Guid> userIds);

    Task<List<TimeConflictUserDto>> GetTimeConflictUsersAsync(
        List<Guid> userIds,
        DateTime start,
        DateTime end);

    Task<List<Guid>> GetUserIdsByRoleAsync(string? roleName);

    Task<User?> GetUserWithSettingsAsync(Guid userId);
    
    Task<List<User>> GetAllActiveUsersWithSettingsAsync();
}