using Application.UseCases.Contests.Dtos;
using Application.UseCases.Contests.Queries;
using Application.UseCases.Contests.Specs;
using Ardalis.Specification;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Application.Common.Helpers;

namespace Application.UseCases.Contests.Queries
{
    public class GetContestLeaderboardHandler
        : IRequestHandler<GetContestLeaderboardQuery, GetContestLeaderboardResponse>
    {
        private readonly IReadRepository<Team, Guid> _teamRepo;
        private readonly IReadRepository<Submission, Guid> _submissionRepo;
        private readonly IReadRepository<ContestProblem, Guid> _cpRepo;

        public GetContestLeaderboardHandler(
            IReadRepository<Team, Guid> teamRepo,
            IReadRepository<Submission, Guid> submissionRepo,
            IReadRepository<ContestProblem, Guid> cpRepo)
        {
            _teamRepo = teamRepo;
            _submissionRepo = submissionRepo;
            _cpRepo = cpRepo;
        }

        public async Task<GetContestLeaderboardResponse> Handle(
            GetContestLeaderboardQuery request,
            CancellationToken cancellationToken)
        {
            var teams = await _teamRepo.ListAsync(new AllTeamsSpec(), cancellationToken);
            var submissions = await _submissionRepo.ListAsync(new AllSubmissionsSpec(), cancellationToken);
            var problems = await _cpRepo.ListAsync(new ContestProblemSpec(request.ContestId), cancellationToken);

            // 🔥 DEBUG
            var start = DateTime.UtcNow;
            Console.WriteLine("===== LEADERBOARD DEBUG START =====");
            Console.WriteLine($"Teams: {teams.Count}");
            Console.WriteLine($"Submissions: {submissions.Count}");
            Console.WriteLine($"Problems: {problems.Count}");

            var contestProblemIds = problems.Select(x => x.Id).ToHashSet();

            Console.WriteLine($"ContestProblemIds: {string.Join(",", contestProblemIds)}");

            var result = new GetContestLeaderboardResponse
            {
                ContestId = request.ContestId,
                Teams = new()
            };

            foreach (var team in teams)
            {
                // ✅ FIX: dùng ContestProblemId (QUAN TRỌNG NHẤT)
                var teamSubs = submissions
                    .Where(x =>
                        x.TeamId == team.Id &&
                        x.ContestProblemId.HasValue &&
                        contestProblemIds.Contains(x.ContestProblemId.Value))
                    .ToList();

                Console.WriteLine($"Team {team.TeamName} => Submissions: {teamSubs.Count}");

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

                    Console.WriteLine($"Problem {p.Id} => Subs: {subs.Count}");

                    int wrong = 0;
                    DateTime? acTime = null;
                    int probPenalty = 0;

                    foreach (var s in subs)
                    {
                        bool isAccepted =
                            !string.IsNullOrEmpty(s.VerdictCode) &&
                            s.VerdictCode.Equals("AC", StringComparison.OrdinalIgnoreCase);

                        if (acTime == null && isAccepted)
                        {
                            acTime = s.CreatedAt;
                            probPenalty = wrong * 20;

                            solved++;
                            penalty += probPenalty;
                        }
                        else if (!isAccepted)
                        {
                            wrong++;
                        }
                    }

                    dto.Problems.Add(new ProblemLeaderboardDto
                    {
                        ProblemId = p.ProblemId,
                        IsSolved = acTime != null,
                        WrongAttempts = wrong,
                        FirstAcAt = acTime,
                        Penalty = acTime != null ? probPenalty : 0,
                        Submissions = new()
                    });
                }

                dto.Solved = solved;
                dto.Penalty = penalty;

                result.Teams.Add(dto);
            }

            result.Teams = result.Teams
                .OrderByDescending(x => x.Solved)
                .ThenBy(x => x.Penalty)
                .ToList();

            Console.WriteLine("===== FINAL RESULT =====");
            foreach (var t in result.Teams)
            {
                Console.WriteLine($"{t.TeamName} => solved={t.Solved}, penalty={t.Penalty}");
            }

            Console.WriteLine($"DEBUG TIME: {(DateTime.UtcNow - start).TotalMilliseconds} ms");
            Console.WriteLine("===== LEADERBOARD DEBUG END =====");

            int rank = 1;
            foreach (var t in result.Teams)
                t.Rank = rank++;

            return result;
        }
    }

    public class AllTeamsSpec : Specification<Team> { }
    public class AllSubmissionsSpec : Specification<Submission> { }
}