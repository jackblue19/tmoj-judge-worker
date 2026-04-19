using MediatR;

namespace Application.Common.Events;

public record ContestFinishedEvent(
    Guid ContestId
) : INotification;