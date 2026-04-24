namespace Application.UseCases.Classes.Dtos;

public record ClassMemberDto(
    Guid MemberId,
    Guid ClassSemesterId,
    Guid UserId,
    string? DisplayName,
    string? Email,
    string? AvatarUrl,
    DateTime JoinedAt,
    bool IsActive);
