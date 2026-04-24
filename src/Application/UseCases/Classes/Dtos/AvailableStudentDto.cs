namespace Application.UseCases.Classes.Dtos;

public record AvailableStudentDto(
    Guid UserId,
    string Email,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? RollNumber,
    string? MemberCode,
    string? AvatarUrl);
