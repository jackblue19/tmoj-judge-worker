using Application.UseCases.Gamification.Dtos;
using MediatR;

namespace Application.UseCases.Gamification.Commands.CreateBadge;

public class CreateBadgeCommand : IRequest<Guid>
{
    public CreateBadgeDto Dto { get; set; } = default!;
}