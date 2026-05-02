namespace Application.UseCases.Classes.Dtos;

public sealed class UpdateClassSemesterRequestDto
{
    public Guid? ClassId { get; init; }
    public Guid? SemesterId { get; init; }
    public Guid? SubjectId { get; init; }
    public Guid? TeacherId { get; init; }
}
