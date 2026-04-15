using Application.UseCases.Contests.Dtos;
using Application.UseCases.Contests.Specs;
using Application.UseCases.Score.Helpers;
using Application.UseCases.Score.Specs;
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
    private readonly IReadRepository<Result, Guid> _resultRepo;
    private readonly IReadRepository<Testcase, Guid> _testcaseRepo;

    public GetContestLeaderboardHandler(
        IReadRepository<ContestTeam, Guid> contestTeamRepo,
        IReadRepository<Submission, Guid> submissionRepo,
        IReadRepository<ContestProblem, Guid> cpRepo,
        IReadRepository<Contest, Guid> contestRepo,
        IReadRepository<Result, Guid> resultRepo,
        IReadRepository<Testcase, Guid> testcaseRepo)
    {
        _contestTeamRepo = contestTeamRepo;
        _submissionRepo = submissionRepo;
        _cpRepo = cpRepo;
        _contestRepo = contestRepo;
        _resultRepo = resultRepo;
        _testcaseRepo = testcaseRepo;
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

        var isFrozen = FreezeContestPatch.IsFrozen(contest);
        var freezeTime = contest.FreezeAt;

        var contestTeams = await _contestTeamRepo.ListAsync(
            new ContestTeamsSpec(request.ContestId), ct);

        var submissions = await _submissionRepo.ListAsync(
            new ContestSubmissionsSpec(request.ContestId), ct);

        var problems = await _cpRepo.ListAsync(
            new ContestProblemSpec(request.ContestId), ct);

        if (isFrozen && freezeTime.HasValue)
        {
            submissions = submissions
                .Where(x => x.CreatedAt <= freezeTime.Value)
                .ToList();
        }

        var isAcm = ScoringHelper.IsAcmContest(contest);

        var response = new GetContestLeaderboardResponse
        {
            ContestId = request.ContestId,
            ScoringMode = isAcm ? "acm" : "ioi",
            Teams = new()
        };

        if (isAcm)
        {
            BuildAcmLeaderboard(response, contest, contestTeams, submissions, problems);
        }
        else
        {
            await BuildIoiLeaderboardAsync(response, contestTeams, submissions, problems, ct);
        }

        return response;
    }

    private static void BuildAcmLeaderboard(
        GetContestLeaderboardResponse response,
        Contest contest,
        IReadOnlyList<ContestTeam> contestTeams,
        IReadOnlyList<Submission> submissions,
        IReadOnlyList<ContestProblem> problems)
    {
        foreach (var ctTeam in contestTeams)
        {
            var team = ctTeam.Team;
            var teamSubs = submissions.Where(x => x.TeamId == team.Id).ToList();

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
                    var isAc = !string.IsNullOrEmpty(s.VerdictCode)
                        && s.VerdictCode.Equals("ac", StringComparison.OrdinalIgnoreCase);

                    if (isAc)
                    {
                        acTime = s.CreatedAt;
                        break;
                    }

                    wrong++;
                }

                int probPenalty = 0;
                if (acTime != null)
                {
                    solved++;
                    var minutes = (int)(acTime.Value - contest.StartAt).TotalMinutes;
                    probPenalty = minutes + wrong * ScoringHelper.AcmPenaltyPerWrong;
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
            response.Teams.Add(dto);
        }

        response.Teams = response.Teams
            .OrderByDescending(x => x.Solved)
            .ThenBy(x => x.Penalty)
            .ToList();

        int rank = 1;
        foreach (var t in response.Teams)
            t.Rank = rank++;
    }

    private async Task BuildIoiLeaderboardAsync(
        GetContestLeaderboardResponse response,
        IReadOnlyList<ContestTeam> contestTeams,
        IReadOnlyList<Submission> submissions,
        IReadOnlyList<ContestProblem> problems,
        CancellationToken ct)
    {
        // Batch load Results + Testcases cho TOÀN BỘ submission của contest — tránh N+1.
        var submissionIds = submissions.Select(s => s.Id).Distinct().ToList();

        var resultsBySubmission = submissionIds.Count == 0
            ? new Dictionary<Guid, List<Result>>()
            : (await _resultRepo.ListAsync(new ResultsBySubmissionIdsSpec(submissionIds), ct))
                .GroupBy(r => r.SubmissionId)
                .ToDictionary(g => g.Key, g => g.ToList());

        var testcaseIds = resultsBySubmission.Values
            .SelectMany(rs => rs)
            .Where(r => r.TestcaseId.HasValue)
            .Select(r => r.TestcaseId!.Value)
            .Distinct()
            .ToList();

        var testcaseInfo = testcaseIds.Count == 0
            ? new Dictionary<Guid, (int Ordinal, int Weight)>()
            : (await _testcaseRepo.ListAsync(new TestcasesByIdsSpec(testcaseIds), ct))
                .ToDictionary(t => t.Id, t => (t.Ordinal, t.Weight));

        foreach (var ctTeam in contestTeams)
        {
            var team = ctTeam.Team;
            var teamSubs = submissions.Where(x => x.TeamId == team.Id).ToList();

            var dto = new TeamLeaderboardDto
            {
                TeamId = team.Id,
                TeamName = team.TeamName,
                Problems = new()
            };

            int totalScore = 0;
            int solved = 0;

            foreach (var p in problems)
            {
                var subs = teamSubs
                    .Where(x => x.ContestProblemId == p.Id)
                    .ToList();

                int bestScore = 0;
                int bestPassed = 0;
                int bestTotal = 0;
                bool anyFullAc = false;

                foreach (var s in subs)
                {
                    if (!resultsBySubmission.TryGetValue(s.Id, out var results) || results.Count == 0)
                        continue;

                    var (score, passed, total, _) = ScoringHelper.CalcIoiScore(results, testcaseInfo);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestPassed = passed;
                        bestTotal = total;
                    }

                    if (total > 0 && passed == total)
                        anyFullAc = true;
                }

                if (anyFullAc)
                    solved++;

                totalScore += bestScore;

                dto.Problems.Add(new ProblemLeaderboardDto
                {
                    ProblemId = p.ProblemId,
                    IsSolved = anyFullAc,
                    Score = bestScore,
                    PassedCases = bestPassed,
                    TotalCases = bestTotal,
                    Submissions = new()
                });
            }

            dto.TotalScore = totalScore;
            dto.Solved = solved;
            response.Teams.Add(dto);
        }

        response.Teams = response.Teams
            .OrderByDescending(x => x.TotalScore)
            .ToList();

        int rank = 1;
        foreach (var t in response.Teams)
            t.Rank = rank++;
    }
}
