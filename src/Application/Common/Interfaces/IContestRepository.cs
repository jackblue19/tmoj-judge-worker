using Application.Common.Models;
using Application.UseCases.Contests.Dtos;
using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IContestRepository
{
    // list
    Task<PagedResult<ContestDto>> GetContestsAsync(
        string? status,
        int page,
        int pageSize);

    // detail
    Task<ContestDetailDto?> GetContestDetailAsync(Guid contestId);

    // check join
    Task<bool> IsTeamJoinedAsync(Guid contestId, Guid teamId);
    Task<bool> IsUserInTeamAsync(Guid userId, Guid teamId);

    // team members
    Task<List<Guid>> GetTeamMemberIdsAsync(Guid teamId);

    // 🔥 ICPC RULE
    Task<bool> HasTimeConflictAsync(Guid userId, DateTime start, DateTime end);

    Task<Contest?> GetActiveContestByTeamIdAsync(Guid teamId);

    Task<ContestTeam?> GetContestTeamAsync(Guid contestId, Guid teamId);
}