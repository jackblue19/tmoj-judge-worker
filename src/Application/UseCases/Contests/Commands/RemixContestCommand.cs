using MediatR;

namespace Application.UseCases.Contests.Commands;

public class RemixContestCommand : IRequest<Guid>
{
    public Guid SourceContestId { get; set; }

    public string? Title { get; set; }

    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }


    public string? VisibilityCode { get; set; }  // public/private/hidden

    public bool IsVirtual { get; set; } = false;
}