namespace Application.UseCases.Classes.Dtos;

public record InviteCodeDto(
    Guid InstanceId,
    string InviteCode,
    DateTime ExpiresAt);
