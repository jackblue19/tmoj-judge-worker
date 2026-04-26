using Application.Common.Interfaces;
using Application.UseCases.Contests.Dtos;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.Contests.Queries;

public class GetContestParticipantsQueryHandler
    : IRequestHandler<GetContestParticipantsQuery, ContestParticipantsResultDto>
{
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly IReadRepository<ContestTeam, Guid> _contestTeamRepo;
    private readonly ICurrentUserService _currentUser;

    public GetContestParticipantsQueryHandler(
        IReadRepository<Contest, Guid> contestRepo,
        IReadRepository<ContestTeam, Guid> contestTeamRepo,
        ICurrentUserService currentUser)
    {
        _contestRepo = contestRepo;
        _contestTeamRepo = contestTeamRepo;
        _currentUser = currentUser;
    }

    public async Task<ContestParticipantsResultDto> Handle(
        GetContestParticipantsQuery request,
        CancellationToken ct)
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("UNAUTHORIZED");

        if (!_currentUser.IsInRole("admin") && !_currentUser.IsInRole("manager"))
            throw new UnauthorizedAccessException("NO_PERMISSION");

        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct)
            ?? throw new KeyNotFoundException("CONTEST_NOT_FOUND");

        var teams = await _contestTeamRepo.ListAsync(
            new ContestTeamsWithMembersSpec(request.ContestId), ct);

        var teamDtos = teams.Select(t => new ContestParticipantTeamDto
        {
            TeamId = t.TeamId,
            TeamName = t.Team.TeamName,
            TeamAvatarUrl = t.Team.AvatarUrl,
            IsPersonal = t.Team.IsPersonal,
            LeaderId = t.Team.LeaderId,
            JoinAt = t.JoinAt,
            Rank = t.Rank,
            Score = t.Score,
            SolvedProblem = t.SolvedProblem,
            Members = t.Team.TeamMembers.Select(tm => new ContestParticipantUserDto
            {
                UserId = tm.UserId,
                DisplayName = tm.User.DisplayName
                    ?? $"{tm.User.FirstName} {tm.User.LastName}".Trim(),
                Email = tm.User.Email,
                AvatarUrl = tm.User.AvatarUrl,
                Username = tm.User.Username,
                RollNumber = tm.User.RollNumber
            }).ToList()
        }).ToList();

        var totalUsers = teams
            .SelectMany(t => t.Team.TeamMembers)
            .Select(tm => tm.UserId)
            .Distinct()
            .Count();

        return new ContestParticipantsResultDto
        {
            TotalTeams = teamDtos.Count,
            TotalUsers = totalUsers,
            Teams = teamDtos
        };
    }
}
