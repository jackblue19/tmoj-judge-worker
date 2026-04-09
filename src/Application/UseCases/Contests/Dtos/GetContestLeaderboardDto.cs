using System;
using System.Collections.Generic;

namespace Application.UseCases.Contests.Queries
{
    public class GetContestLeaderboardResponse
    {
        public Guid ContestId { get; set; }
        public List<TeamLeaderboardDto> Teams { get; set; } = new();
    }

    public class TeamLeaderboardDto
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;

        public int Solved { get; set; }
        public int Penalty { get; set; }
        public int Rank { get; set; }

        public List<ProblemLeaderboardDto> Problems { get; set; } = new();
    }

    public class ProblemLeaderboardDto
    {
        public Guid ProblemId { get; set; }

        public bool IsSolved { get; set; }
        public int WrongAttempts { get; set; }

        public DateTime? FirstAcAt { get; set; }
        public int Penalty { get; set; }

        public List<SubmissionTimelineDto> Submissions { get; set; } = new();
    }

    public class SubmissionTimelineDto
    {
        public Guid SubmissionId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public bool IsAccepted { get; set; }

    }
}