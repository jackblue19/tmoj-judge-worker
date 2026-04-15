using MediatR;

namespace Application.UseCases.Contests.Commands;

public class UpdateContestCommand : IRequest<bool>
{
    public Guid ContestId { get; set; }

    public string Title { get; set; } = "";
    public string Description { get; set; } = "";

    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }

    public string VisibilityCode { get; set; } = "private";
    public string? ContestType { get; set; }
    public bool AllowTeams { get; set; }
}