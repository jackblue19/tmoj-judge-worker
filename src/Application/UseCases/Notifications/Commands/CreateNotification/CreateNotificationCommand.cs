using Application.UseCases.Notifications.Dtos;
using MediatR;
using System;

namespace Application.UseCases.Notifications.Commands.CreateNotification;

public class CreateNotificationCommand : IRequest<NotificationDto>
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = null!;
    public string? Message { get; set; }
    public string Type { get; set; } = null!;
    public string? ScopeType { get; set; }
    public Guid? ScopeId { get; set; }
    public Guid? CreatedBy { get; set; }
}
