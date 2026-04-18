using Application.Common.Models;
using Application.UseCases.Contests.Dtos;
using Domain.Entities;

namespace Application.Common.Interfaces;

public interface IContestRepository
{
    // =============================================
    // LIST
    // =============================================
    Task<PagedResult<ContestDto>> GetContestsAsync(
        string? status,
        string? visibilityCode,
        bool includeArchived,
        int page,
        int pageSize);

    // =============================================
    // DETAIL
    // =============================================
    Task<ContestDetailDto?> GetContestDetailAsync(Guid contestId);

    // =============================================
    // TEAM / JOIN
    // =============================================
    Task<bool> IsTeamJoinedAsync(Guid contestId, Guid teamId);
    Task<bool> IsUserInTeamAsync(Guid userId, Guid teamId);
    Task<List<Guid>> GetTeamMemberIdsAsync(Guid teamId);

    // =============================================
    // ICPC RULE
    // =============================================
    Task<bool> HasTimeConflictAsync(Guid userId, DateTime start, DateTime end);

    // =============================================
    // CONTEST CONTEXT
    // =============================================
    Task<Contest?> GetActiveContestByTeamIdAsync(Guid teamId);
    Task<ContestTeam?> GetContestTeamAsync(Guid contestId, Guid teamId);

    // =============================================
    // MY CONTESTS
    // =============================================
    Task<List<Contest>> GetMyContestsAsync(Guid userId);
    Task<List<MyContestDto>> GetMyContestsDetailedAsync(Guid userId);
    Task<MyTeamInContestDto?> GetMyTeamInContestAsync(Guid contestId, Guid userId);

    // =============================================
    // 🔥 SCOREBOARD (ICPC)
    // =============================================
    Task<List<ContestScoreboardDto>> GetScoreboardAsync(Guid contestId);
}