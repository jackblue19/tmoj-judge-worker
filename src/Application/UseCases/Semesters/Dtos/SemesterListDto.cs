namespace Application.UseCases.Semesters.Dtos;

public record SemesterListDto(
    List<SemesterDto> Items,
    int TotalCount
);
