using Application.Common.Interfaces;
using Application.UseCases.Teams.Dtos;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Common.Repositories;

public class TeamRepository : ITeamRepository
{
    private readonly TmojDbContext _db;

    public TeamRepository(TmojDbContext db)
    {
        _db = db;
    }

    // =========================
    // GET TEAM DETAIL
    // =========================
    public async Task<TeamDto?> GetTeamDetailAsync(Guid teamId)
    {
        var team = await _db.Teams
            .AsNoTracking()
            .Include(x => x.TeamMembers)
                .ThenInclude(tm => tm.User)
            .FirstOrDefaultAsync(x => x.Id == teamId);

        if (team == null) return null;

        return new TeamDto
        {
            Id = team.Id,
            TeamName = team.TeamName,
            LeaderId = team.LeaderId,
            IsPersonal = team.IsPersonal,
            InviteCode = team.InviteCode,
            CreatedAt = team.CreatedAt,
            TeamSize = team.TeamMembers.Count,

            Members = team.TeamMembers.Select(m => new TeamMemberDto
            {
                UserId = m.UserId,
                UserName =
                    !string.IsNullOrEmpty(m.User.DisplayName)
                        ? m.User.DisplayName
                        : !string.IsNullOrEmpty(m.User.FirstName)
                            ? $"{m.User.FirstName} {m.User.LastName}"
                            : m.User.Username,
                JoinedAt = m.JoinedAt
            }).ToList()
        };
    }

    // =========================
    // GET USER TEAMS
    // =========================
    public async Task<List<TeamDto>> GetTeamsByUserAsync(Guid userId)
    {
        return await _db.TeamMembers
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new TeamDto
            {
                Id = x.Team.Id,
                TeamName = x.Team.TeamName,
                LeaderId = x.Team.LeaderId,
                IsPersonal = x.Team.IsPersonal,
                InviteCode = x.Team.InviteCode,
                CreatedAt = x.Team.CreatedAt,
                TeamSize = x.Team.TeamMembers.Count
            })
            .ToListAsync();
    }

    // =========================
    // CHECK LEADER
    // =========================
    public async Task<bool> IsUserLeaderAsync(Guid userId, Guid teamId)
    {
        return await _db.Teams
            .AnyAsync(x => x.Id == teamId && x.LeaderId == userId);
    }

    // =========================
    // CHECK MEMBER
    // =========================
    public async Task<bool> IsUserInTeamAsync(Guid userId, Guid teamId)
    {
        return await _db.TeamMembers
            .AnyAsync(x => x.TeamId == teamId && x.UserId == userId);
    }

    // =========================
    // GET TEAM MEMBER
    // =========================
    public async Task<TeamMember?> GetTeamMemberAsync(Guid teamId, Guid userId)
    {
        return await _db.TeamMembers
            .Include(x => x.Team)
            .FirstOrDefaultAsync(x =>
                x.TeamId == teamId &&
                x.UserId == userId);
    }

    // =========================
    // 🔥 FIX: GET TEAM MEMBERS
    // =========================
    public async Task<List<TeamMember>> GetTeamMembersAsync(Guid teamId)
    {
        return await _db.TeamMembers
            .AsNoTracking()
            .Where(x => x.TeamId == teamId)
            .ToListAsync();
    }

    // =========================
    // DELETE MEMBER
    // =========================
    public void DeleteTeamMember(TeamMember member)
    {
        _db.TeamMembers.Remove(member);
    }

    // =========================
    // COUNT MEMBERS
    // =========================
    public async Task<int> GetTeamMemberCountAsync(Guid teamId)
    {
        return await _db.TeamMembers
            .CountAsync(x => x.TeamId == teamId);
    }

    // =========================
    // VALIDATION
    // =========================
    public async Task<bool> IsTeamValidForContestAsync(Guid teamId)
    {
        var count = await _db.TeamMembers
            .CountAsync(x => x.TeamId == teamId);

        return count >= 3 && count <= 5;
    }

    // =========================
    // INVITE CODE
    // =========================
    public async Task<Team?> GetByInviteCodeAsync(string code)
    {
        return await _db.Teams
            .FirstOrDefaultAsync(x => x.InviteCode == code);
    }

    // =========================
    // CONTEST BUSY CHECK
    // =========================
    public async Task<bool> IsUserBusyInContestAsync(Guid userId, Guid contestId)
    {
        var contest = await _db.Contests
            .FirstOrDefaultAsync(x => x.Id == contestId);

        if (contest == null)
            throw new Exception("Contest not found");

        var userContests = await _db.TeamMembers
            .Where(tm => tm.UserId == userId)
            .SelectMany(tm => tm.Team.ContestTeams)
            .Select(ct => ct.Contest)
            .ToListAsync();

        return userContests.Any(c =>
            !(c.EndAt <= contest.StartAt || c.StartAt >= contest.EndAt));
    }

    // =========================
    // ADMIN
    // =========================
    public async Task<List<TeamDto>> GetAllTeamsAsync()
    {
        return await _db.Teams
            .AsNoTracking()
            .Include(t => t.TeamMembers)
                .ThenInclude(tm => tm.User)
            .Select(t => new TeamDto
            {
                Id = t.Id,
                TeamName = t.TeamName,
                LeaderId = t.LeaderId,
                IsPersonal = t.IsPersonal,
                InviteCode = t.InviteCode,
                CreatedAt = t.CreatedAt,
                TeamSize = t.TeamMembers.Count,

                Members = t.TeamMembers.Select(m => new TeamMemberDto
                {
                    UserId = m.UserId,
                    UserName =
                        !string.IsNullOrEmpty(m.User.DisplayName)
                            ? m.User.DisplayName
                            : !string.IsNullOrEmpty(m.User.FirstName)
                                ? $"{m.User.FirstName} {m.User.LastName}"
                                : m.User.Username,
                    JoinedAt = m.JoinedAt
                }).ToList()
            })
            .ToListAsync();
    }

    // =========================
    // GET PERSONAL TEAM
    // =========================
    public async Task<Team?> GetPersonalTeamAsync(Guid userId)
    {
        return await _db.Teams
            .Include(t => t.TeamMembers)
            .FirstOrDefaultAsync(t =>
                t.IsPersonal &&
                t.LeaderId == userId);
    }
}