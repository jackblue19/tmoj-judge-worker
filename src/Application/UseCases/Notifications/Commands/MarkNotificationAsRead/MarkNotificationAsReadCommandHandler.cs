using Application.Common.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Notifications.Commands.MarkNotificationAsRead;

public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, bool>
{
    private readonly IReadRepository<Notification, Guid> _readRepo;
    private readonly IWriteRepository<Notification, Guid> _writeRepo;
    private readonly IUnitOfWork _uow;

    public MarkNotificationAsReadCommandHandler(
        IReadRepository<Notification, Guid> readRepo,
        IWriteRepository<Notification, Guid> writeRepo,
        IUnitOfWork uow)
    {
        _readRepo = readRepo;
        _writeRepo = writeRepo;
        _uow = uow;
    }

    public async Task<bool> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _readRepo.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification == null)
            throw new Exception("Notification not found");

        notification.IsRead = true;
        _writeRepo.Update(notification);
        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
