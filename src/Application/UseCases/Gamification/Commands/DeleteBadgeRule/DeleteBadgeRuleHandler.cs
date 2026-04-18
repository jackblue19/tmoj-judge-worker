using Application.Common.Interfaces;
using MediatR;

namespace Application.UseCases.Gamification.Commands.DeleteBadgeRule;

public class DeleteBadgeRuleHandler : IRequestHandler<DeleteBadgeRuleCommand, bool>
{
    private readonly IGamificationRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public DeleteBadgeRuleHandler(
        IGamificationRepository repo,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(DeleteBadgeRuleCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsInRole("Admin"))
            throw new UnauthorizedAccessException();

        var rule = await _repo.GetBadgeRuleByIdAsync(request.Id);
        if (rule == null) return false;

        rule.IsActive = false;

        await _repo.UpdateBadgeRuleAsync(rule);
        await _repo.SaveChangesAsync();

        return true;
    }
}