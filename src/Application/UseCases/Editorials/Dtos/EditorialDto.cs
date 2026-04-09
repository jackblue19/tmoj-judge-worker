namespace Application.UseCases.Editorials.Dtos;

public record EditorialDto(
    Guid EditorialId,
    Guid ProblemId,
    Guid? AuthorId,
    string FilePath,
    string FileType,
    DateTime CreatedAt,
    DateTime? UpdatedAt = null
);
