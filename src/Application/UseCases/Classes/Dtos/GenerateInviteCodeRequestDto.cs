namespace Application.UseCases.Classes.Dtos;

public sealed class GenerateInviteCodeRequestDto
{
    public int MinutesValid { get; init; } = 15;
}
