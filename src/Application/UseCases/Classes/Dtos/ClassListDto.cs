namespace Application.UseCases.Classes.Dtos;

public record ClassListDto(
    List<ClassDto> Items,
    int TotalCount);
