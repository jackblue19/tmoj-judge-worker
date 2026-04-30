using MediatR;
using System;

namespace Application.UseCases.Gamification.Commands.MarkBadgeNotified;

public record MarkBadgeNotifiedCommand(Guid BadgeId) : IRequest<bool>;
