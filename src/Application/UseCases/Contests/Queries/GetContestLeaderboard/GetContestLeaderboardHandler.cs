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
        if (request.ContestId == Guid.Empty)
            throw new ArgumentException("ContestId is required");

        var contest = await _contestRepo.GetByIdAsync(request.ContestId, ct);
        if (contest == null)
            throw new Exception("Contest not found");

        // Determine contest status
        var now = DateTime.UtcNow;
        var status = now < contest.StartAt ? "upcoming"
                   : now > contest.EndAt ? "ended"
                   : "running";

        // Freeze state
        var isPrivileged =
            _currentUser.IsAuthenticated &&
            (_currentUser.IsInRole("admin") || _currentUser.IsInRole("manager"));
        var isFrozen = !isPrivileged && FreezeContestPatch.IsFrozen(contest);
        var freezeTime = contest.FreezeAt;

        // Fetch data
        var contestTeams = await _contestTeamRepo.ListAsync(
            new ContestTeamsSpec(request.ContestId), ct);

        var submissions = await _submissionRepo.ListAsync(
            new ContestSubmissionsSpec(request.ContestId), ct);

        var problems = (await _cpRepo.ListAsync(
                new ContestProblemSpec(request.ContestId), ct))
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayIndex ?? p.Ordinal ?? 999)
            .ToList();

        // Apply freeze filter
        if (isFrozen && freezeTime.HasValue)
        {
            submissions = submissions
                .Where(x => x.CreatedAt <= freezeTime.Value)
                .ToList();
        }

        var isAcm = contest.ContestType?.ToLower() == "acm";

        var problemStats = new Dictionary<Guid, (int solvedCount, int totalAttempts)>();

        // Initialize problem stats
        foreach (var p in problems)
        {
            problemStats[p.Id] = (0, 0);
        }

        static (Guid userId, string name, string? avatar) ResolveParticipant(Team team)
        {
            if (team.IsPersonal)
            {
                var member = team.TeamMembers.FirstOrDefault();
                var user = member?.User;
                return (user?.Id ?? team.Id, user?.DisplayName ?? team.TeamName, user?.AvatarUrl);
            }
            return (team.Id, team.TeamName, null);
        }

        // Build appropriate scoreboard based on mode
        if (isAcm)
        {
            var acmRows = new List<ACMScoreboardRowDto>();

            foreach (var ctTeam in contestTeams)
            {
                var team = ctTeam.Team;
                if (team == null) continue;

                var teamSubs = submissions.Where(x => x.TeamId == team.Id).ToList();
                int totalSolved = 0;
                int totalPenalty = 0;
                var problemAttempts = new List<ACMProblemAttemptDto>();

                foreach (var p in problems)
                {
                    var subs = teamSubs
                        .Where(x => x.ContestProblemId == p.Id)
                        .OrderBy(x => x.CreatedAt)
                        .ToList();

                    int wrongCount = 0;
                    DateTime? acTime = null;
                    bool isFirstBlood = false;

                    foreach (var s in subs)
                    {
                        var verdict = s.VerdictCode?.ToUpper();
                        if (verdict == "AC")
                        {
                            acTime = s.CreatedAt;
                            break;
                        }
                        else if (verdict == "WA" || verdict == "TLE" || verdict == "MLE" || verdict == "RE")
                        {
                            wrongCount++;
                        }
                    }

                    int probPenalty = 0;

                    if (acTime != null)
                    {
                        totalSolved++;
                        probPenalty = (int)(acTime.Value - contest.StartAt).TotalMinutes + wrongCount * 20;
                        totalPenalty += probPenalty;

                        // Check if first blood
                        var firstBloodSub = submissions
                            .Where(x => x.ContestProblemId == p.Id && x.VerdictCode?.ToUpper() == "AC")
                            .OrderBy(x => x.CreatedAt)
                            .FirstOrDefault();
                        isFirstBlood = firstBloodSub?.TeamId == team.Id;

                        // Update solved count
                        var (solved, attempts) = problemStats[p.Id];
                        problemStats[p.Id] = (solved + 1, attempts + subs.Count);
                    }
                    else if (subs.Any())
                    {
                        var (solved, attempts) = problemStats[p.Id];
                        problemStats[p.Id] = (solved, attempts + subs.Count);
                    }

                    problemAttempts.Add(new ACMProblemAttemptDto
                    {
                        ProblemId = p.DisplayIndex?.ToString() ?? (problems.IndexOf(p) + 1).ToString(),
                        IsSolved = acTime != null,
                        IsFirstBlood = isFirstBlood,
                        AttemptsCount = subs.Count,
                        PenaltyTime = probPenalty > 0 ? probPenalty : null
                    });
                }

                var (acmUserId, acmName, acmAvatar) = ResolveParticipant(team);
                acmRows.Add(new ACMScoreboardRowDto
                {
                    Rank = 0,
                    UserId = acmUserId,
                    Username = acmName,
                    AvatarUrl = acmAvatar,
                    Fullname = acmName,
                    TotalSolved = totalSolved,
                    TotalPenalty = totalPenalty,
                    Problems = problemAttempts
                });
            }

            // Sort and rank ACM
            acmRows = acmRows.OrderByDescending(x => x.TotalSolved)
                             .ThenBy(x => x.TotalPenalty)
                             .ToList();

            int rank = 1;
            foreach (var row in acmRows)
            {
                row.Rank = rank++;
            }

            var acmResult = new ACMScoreboardResponse
            {
                ContestId = request.ContestId,
                ContestName = contest.Title,
                ScoringMode = "acm",
                Status = status,
                Frozen = isFrozen,
                Problems = problems.Select((p, idx) =>
                {
                    var (solvedCount, totalAttempts) = problemStats[p.Id];
                    return new ContestProblemHeaderDto
                    {
                        Id = p.DisplayIndex?.ToString() ?? (idx + 1).ToString(),
                        Title = p.Problem?.Title ?? "Unknown",
                        BalloonColor = null,
                        SolvedCount = solvedCount,
                        TotalAttempts = totalAttempts
                    };
                }).ToList(),
                Rows = acmRows,
                LastUpdated = DateTime.UtcNow.ToString("O")
            };

            return acmResult;
        }
        else
        {
            var ioiRows = new List<IOIScoreboardRowDto>();

            foreach (var ctTeam in contestTeams)
            {
                var team = ctTeam.Team;
                if (team == null) continue;

                var teamSubs = submissions.Where(x => x.TeamId == team.Id).ToList();
                int totalScore = 0;
                var problemAttempts = new List<IOIProblemAttemptDto>();

                foreach (var p in problems)
                {
                    var subs = teamSubs
                        .Where(x => x.ContestProblemId == p.Id)
                        .OrderByDescending(x => x.FinalScore ?? 0)
                        .ToList();

                    int bestScore = 0;

                    foreach (var s in subs)
                    {
                        if (s.FinalScore.HasValue)
                        {
                            bestScore = Math.Max(bestScore, (int)s.FinalScore.Value);
                        }
                    }

                    totalScore += bestScore;

                    if (subs.Any())
                    {
                        var (solved, attempts) = problemStats[p.Id];
                        if (bestScore > 0)
                            problemStats[p.Id] = (solved + 1, attempts + subs.Count);
                        else
                            problemStats[p.Id] = (solved, attempts + subs.Count);
                    }

                    problemAttempts.Add(new IOIProblemAttemptDto
                    {
                        ProblemId = p.DisplayIndex?.ToString() ?? (problems.IndexOf(p) + 1).ToString(),
                        Score = bestScore,
                        AttemptsCount = subs.Count
                    });
                }

                var (ioiUserId, ioiName, ioiAvatar) = ResolveParticipant(team);
                ioiRows.Add(new IOIScoreboardRowDto
                {
                    Rank = 0,
                    UserId = ioiUserId,
                    Username = ioiName,
                    AvatarUrl = ioiAvatar,
                    Fullname = ioiName,
                    TotalScore = totalScore,
                    Problems = problemAttempts
                });
            }

            // Sort and rank IOI
            ioiRows = ioiRows.OrderByDescending(x => x.TotalScore).ToList();

            int rank = 1;
            foreach (var row in ioiRows)
            {
                row.Rank = rank++;
            }

            var ioiResult = new IOIScoreboardResponse
            {
                ContestId = request.ContestId,
                ContestName = contest.Title,
                ScoringMode = "ioi",
                Status = status,
                Frozen = isFrozen,
                Problems = problems.Select((p, idx) =>
                {
                    var (solvedCount, totalAttempts) = problemStats[p.Id];
                    return new ContestProblemHeaderDto
                    {
                        Id = p.DisplayIndex?.ToString() ?? (idx + 1).ToString(),
                        Title = p.Problem?.Title ?? "Unknown",
                        BalloonColor = null,
                        SolvedCount = solvedCount,
                        TotalAttempts = totalAttempts
                    };
                }).ToList(),
                Rows = ioiRows,
                LastUpdated = DateTime.UtcNow.ToString("O")
            };

            return ioiResult;
        }
    }
}