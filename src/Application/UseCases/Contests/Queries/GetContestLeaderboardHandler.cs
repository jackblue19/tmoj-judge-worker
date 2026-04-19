using Application.Common.Interfaces;
using Application.UseCases.Contests.Dtos;
using Application.UseCases.Contests.Specs;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Policies;

namespace Application.UseCases.Contests.Queries;

public class GetContestLeaderboardHandler
    : IRequestHandler<GetContestLeaderboardQuery, GetContestLeaderboardResponse>
{
    private readonly IReadRepository<ContestTeam, Guid> _contestTeamRepo;
    private readonly IReadRepository<Submission, Guid> _submissionRepo;
    private readonly IReadRepository<ContestProblem, Guid> _cpRepo;
    private readonly IReadRepository<Contest, Guid> _contestRepo;
    private readonly ICurrentUserService _currentUser;

    public GetContestLeaderboardHandler(
        IReadRepository<ContestTeam, Guid> contestTeamRepo,
        IReadRepository<Submission, Guid> submissionRepo,
        IReadRepository<ContestProblem, Guid> cpRepo,
        IReadRepository<Contest, Guid> contestRepo,
        ICurrentUserService currentUser)
    {
        _contestTeamRepo = contestTeamRepo;
        _submissionRepo = submissionRepo;
        _cpRepo = cpRepo;
        _contestRepo = contestRepo;
        _currentUser = currentUser;
    }

    public async Task<GetContestLeaderboardResponse> Handle(
        GetContestLeaderboardQuery request,
        CancellationToken ct)
    {
        Console.WriteLine("===== SCOREBOARD START =====");
        Console.WriteLine($"ContestId: {request.ContestId}");

        if (request.ContestId == Guid.Empty)
            throw new ArgumentException("ContestId is required");

        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);

        if (contest == null)
            throw new Exception("Contest not found");

        Console.WriteLine($"Contest: {contest.Title}");

        // ======================
        // FREEZE STATE
        // ======================
        // Rule 4.4/9: admin/manager luôn thấy scoreboard thật (bypass freeze).
        var isPrivileged =
            _currentUser.IsAuthenticated &&
            (_currentUser.IsInRole("admin") || _currentUser.IsInRole("manager"));

        var isFrozen = !isPrivileged && FreezeContestPatch.IsFrozen(contest);
        var freezeTime = contest.FreezeAt;

        // ======================
        // FETCH DATA
        // ======================
        var contestTeams = await _contestTeamRepo.ListAsync(
            new ContestTeamsSpec(request.ContestId), ct);

        var submissions = await _submissionRepo.ListAsync(
            new ContestSubmissionsSpec(request.ContestId), ct);

        var problems = (await _cpRepo.ListAsync(
                new ContestProblemSpec(request.ContestId), ct))
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayIndex ?? p.Ordinal ?? 999)
            .ToList();

        // ======================
        // APPLY FREEZE (CORE LOGIC)
        // ======================
        if (isFrozen && freezeTime.HasValue)
        {
            submissions = submissions
                .Where(x => x.CreatedAt <= freezeTime.Value)
                .ToList();
        }

        Console.WriteLine($"Teams: {contestTeams.Count}");
        Console.WriteLine($"Submissions (after freeze filter): {submissions.Count}");
        Console.WriteLine($"Problems: {problems.Count}");

        var result = new GetContestLeaderboardResponse
        {
            ContestId = request.ContestId,
            Teams = new()
        };

        // ======================
        // BUILD SCOREBOARD
        // ======================
        foreach (var ctTeam in contestTeams)
        {
            var team = ctTeam.Team;
            if (team == null) continue;

            var teamSubs = submissions
                .Where(x => x.TeamId == team.Id)
                .ToList();

            var dto = new TeamLeaderboardDto
            {
                TeamId = team.Id,
                TeamName = team.TeamName,
                Problems = new()
            };

            int solved = 0;
            int penalty = 0;

            foreach (var p in problems)
            {
                var subs = teamSubs
                    .Where(x => x.ContestProblemId == p.Id)
                    .OrderBy(x => x.CreatedAt)
                    .ToList();

                int wrong = 0;
                DateTime? acTime = null;

                foreach (var s in subs)
                {
                    var verdict = s.VerdictCode?.ToUpper();

                    if (verdict == "AC")
                    {
                        acTime = s.CreatedAt;
                        break;
                    }

                    // chỉ tính penalty cho fail hợp lệ
                    if (verdict == "WA" || verdict == "TLE" || verdict == "MLE" || verdict == "RE")
                    {
                        wrong++;
                    }
                }

                int probPenalty = 0;

                if (acTime != null)
                {
                    solved++;

                    var minutes =
                        (int)(acTime.Value - contest.StartAt).TotalMinutes;

                    probPenalty = minutes + wrong * 20;

                    penalty += probPenalty;
                }

                dto.Problems.Add(new ProblemLeaderboardDto
                {
                    ProblemId = p.ProblemId,
                    IsSolved = acTime != null,
                    WrongAttempts = wrong,
                    FirstAcAt = acTime,
                    Penalty = probPenalty,
                    Submissions = new()
                });
            }

            dto.Solved = solved;
            dto.Penalty = penalty;

            result.Teams.Add(dto);
        }

        // ======================
        // SORT + RANK
        // ======================
        result.Teams = result.Teams
            .OrderByDescending(x => x.Solved)
            .ThenBy(x => x.Penalty)
            .ToList();

        int rank = 1;
        foreach (var t in result.Teams)
        {
            t.Rank = rank++;
        }

        Console.WriteLine("===== SCOREBOARD END =====");

        return result;
    }
}