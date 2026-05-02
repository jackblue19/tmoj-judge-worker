namespace Application.UseCases.Classes.Dtos;

public sealed class CreateClassRequestDto
{
    public string ClassCode { get; init; } = string.Empty;
    public Guid SubjectId { get; init; }
    public Guid SemesterId { get; init; }
    public Guid? TeacherId { get; init; }
}
