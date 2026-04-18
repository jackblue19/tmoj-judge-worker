using MediatR;

namespace Application.Common.Events;

public record ProblemSolvedEvent(
    Guid UserId,
    Guid ProblemId,
    Guid SubmissionId
) : INotification;