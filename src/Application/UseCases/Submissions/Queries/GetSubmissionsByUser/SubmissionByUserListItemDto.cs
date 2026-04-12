namespace Application.UseCases.Submissions.Queries.GetSubmissionsByUser;

public sealed record SubmissionByUserListItemDto(
    Guid Id ,
    Guid UserId ,
    Guid ProblemId ,
    Guid? RuntimeId ,
    string StatusCode ,
    string? VerdictCode ,
    decimal? FinalScore ,
    int? TimeMs ,
    int? MemoryKb ,
    DateTime CreatedAt ,
    DateTime? JudgedAt
);