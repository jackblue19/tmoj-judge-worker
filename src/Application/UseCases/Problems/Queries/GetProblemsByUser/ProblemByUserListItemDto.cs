namespace Application.UseCases.Problems.Queries.GetProblemsByUser;

public sealed record ProblemByUserListItemDto(
    Guid ProblemId ,
    string Slug ,
    string Title ,
    string? Difficulty ,
    string? TypeCode ,
    bool IsActive ,
    int Attempts ,
    bool Solved ,
    Guid? BestSubmissionId ,
    DateTime? LastSubmissionAt
);