namespace Application.UseCases.Classes.Dtos;

public record ClassTeacherDto(
    Guid UserId,
    string? DisplayName,
    string? Email,
    string? AvatarUrl);
