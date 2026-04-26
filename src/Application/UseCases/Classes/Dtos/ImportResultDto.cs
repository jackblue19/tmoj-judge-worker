namespace Application.UseCases.Classes.Dtos;

public record ImportResultDto(
    int TotalProcessed,
    int SuccessCount,
    int FailedCount,
    List<string> Errors);
