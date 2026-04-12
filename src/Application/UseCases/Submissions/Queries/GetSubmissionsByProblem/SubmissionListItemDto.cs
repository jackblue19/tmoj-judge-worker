namespace Application.UseCases.Submissions.Queries.GetSubmissionsByProblem;

public sealed record SubmissionListItemDto(
    Guid Id ,
    Guid UserId ,
    Guid ProblemId ,
    string StatusCode ,
    string? VerdictCode ,
    decimal? FinalScore ,
    int? TimeMs ,
    int? MemoryKb ,
    DateTime CreatedAt ,
    DateTime? JudgedAt
);