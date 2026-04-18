using MediatR;

namespace Application.UseCases.Gamification.Events;

public class SubmissionAcceptedEvent : INotification
{
    public Guid UserId { get; }
    public Guid ProblemId { get; }
    public DateTime OccurredAt { get; }

    public SubmissionAcceptedEvent(Guid userId, Guid problemId)
    {
        UserId = userId;
        ProblemId = problemId;
        OccurredAt = DateTime.UtcNow;
    }
}