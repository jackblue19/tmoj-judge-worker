using System;
using System.Collections.Generic;

namespace Application.UseCases.Contests.Queries
{
    public class GetContestLeaderboardResponse
    {
        public Guid ContestId { get; set; }

        /// <summary>"acm" hoặc "ioi" — quyết định bởi Contest.ContestType.</summary>
        public string ScoringMode { get; set; } = "ioi";

        public List<TeamLeaderboardDto> Teams { get; set; } = new();
    }

    public class TeamLeaderboardDto
    {
        public Guid TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;

        // ACM fields (ý nghĩa khi ScoringMode == "acm")
        public int Solved { get; set; }
        public int Penalty { get; set; }

        // IOI field (ý nghĩa khi ScoringMode == "ioi") — tổng điểm weight của best submission từng problem.
        public int TotalScore { get; set; }

        public int Rank { get; set; }

        public List<ProblemLeaderboardDto> Problems { get; set; } = new();
    }

    public class ProblemLeaderboardDto
    {
        public Guid ProblemId { get; set; }

        // ACM-specific
        public bool IsSolved { get; set; }
        public int WrongAttempts { get; set; }
        public DateTime? FirstAcAt { get; set; }
        public int Penalty { get; set; }

        // IOI-specific: điểm weight của best submission cho problem này.
        public int Score { get; set; }
        public int PassedCases { get; set; }
        public int TotalCases { get; set; }

        public List<SubmissionTimelineDto> Submissions { get; set; } = new();
    }

    public class SubmissionTimelineDto
    {
        public Guid SubmissionId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public bool IsAccepted { get; set; }

    }
}