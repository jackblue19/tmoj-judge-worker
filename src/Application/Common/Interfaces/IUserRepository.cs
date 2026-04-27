using Domain.Entities;
using Application.UseCases.Contests.Dtos;

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
}