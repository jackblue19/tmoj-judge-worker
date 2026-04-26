using MediatR;

namespace Application.UseCases.Contests.Commands;

public class RegisterContestCommand : IRequest<Guid>
{
    public Guid ContestId { get; set; }

    // chọn mode
    public bool IsTeam { get; set; }

    // nếu team
    public string? TeamName { get; set; }
    public List<Guid>? MemberIds { get; set; }
}