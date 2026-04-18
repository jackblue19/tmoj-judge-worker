using MediatR;

namespace Application.UseCases.Gamification.Commands.DeleteBadgeRule;

public class DeleteBadgeRuleCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}