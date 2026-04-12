using Application.UseCases.Teams.Dtos;
using Domain.Entities;

namespace Application.Common.Interfaces;

public interface ITeamRepository
{
    // =========================
    // DETAIL
    // =========================
    Task<TeamDto?> GetTeamDetailAsync(Guid teamId);

    // =========================
    // LIST USER TEAMS
    // =========================
    Task<List<TeamDto>> GetTeamsByUserAsync(Guid userId);

    // =========================
    // MEMBER CHECK
    // =========================
    Task<bool> IsUserLeaderAsync(Guid userId, Guid teamId);

    Task<bool> IsUserInTeamAsync(Guid userId, Guid teamId);

    // =========================
    // MEMBER COUNT
    // =========================
    Task<int> GetTeamMemberCountAsync(Guid teamId);

    // =========================
    // TEAM VALIDATION
    // =========================
    Task<bool> IsTeamValidForContestAsync(Guid teamId);

    // =========================
    // TEAM MEMBER OPS
    // =========================
    Task<TeamMember?> GetTeamMemberAsync(Guid teamId, Guid userId);

    void DeleteTeamMember(TeamMember member);

    Task<Team?> GetByInviteCodeAsync(string code);
}