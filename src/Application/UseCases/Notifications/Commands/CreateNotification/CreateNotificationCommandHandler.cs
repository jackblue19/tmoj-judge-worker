using Application.Common.Interfaces;
using Application.UseCases.Notifications.Dtos;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Notifications.Commands.CreateNotification;

public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, NotificationDto>
{
    private readonly IWriteRepository<Notification, Guid> _writeRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CreateNotificationCommandHandler> _logger;

    public CreateNotificationCommandHandler(
        IWriteRepository<Notification, Guid> writeRepo,
        IUnitOfWork uow,
        ILogger<CreateNotificationCommandHandler> logger)
    {
        _writeRepo = writeRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task<NotificationDto> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        try 
        {
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = request.UserId,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                ScopeType = request.ScopeType,
                ScopeId = request.ScopeId,
                CreatedBy = request.CreatedBy,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _writeRepo.AddAsync(notification, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            return NotificationDto.FromEntity(notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR creating notification for User={UserId}, Title={Title}", request.UserId, request.Title);
            throw;
        }
    }
}
