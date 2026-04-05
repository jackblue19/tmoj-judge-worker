namespace Application.UseCases.Editorials;

public record EditorialDto(
    Guid EditorialId,
    Guid ProblemId,
    Guid? AuthorId,
    string FilePath,
    string FileType,
    DateTime CreatedAt,
    DateTime? UpdatedAt = null
);
