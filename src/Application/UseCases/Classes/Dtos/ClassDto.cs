namespace Application.UseCases.Classes.Dtos;

public record ClassDto(
    Guid ClassId,
    string ClassCode,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<ClassInstanceDto> Instances,
    int TotalMemberCount);
