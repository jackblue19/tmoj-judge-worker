using Domain.Entities;
using System;

namespace Application.UseCases.Notifications.Dtos;

public class NotificationDto
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = null!;
    public string? Message { get; set; }
    public string Type { get; set; } = null!;
    public string? ScopeType { get; set; }
    public Guid? ScopeId { get; set; }
    public bool IsRead { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public static NotificationDto FromEntity(Notification entity)
    {
        return new NotificationDto
        {
            NotificationId = entity.NotificationId,
            UserId = entity.UserId,
            Title = entity.Title,
            Message = entity.Message,
            Type = entity.Type,
            ScopeType = entity.ScopeType,
            ScopeId = entity.ScopeId,
            IsRead = entity.IsRead,
            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt
        };
    }
}
