namespace Application.UseCases.Classes.Dtos;

public sealed class AddStudentManuallyRequestDto
{
    public string? RollNumber { get; init; }
    public string? MemberCode { get; init; }
}
