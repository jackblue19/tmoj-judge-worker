namespace Application.UseCases.Classes.Dtos;

public record PagedAvailableStudentsDto(
    List<AvailableStudentDto> Items,
    int TotalCount);
