using Application.Common.Interfaces;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Announcements.Commands.DeleteAnnouncement;

public class DeleteAnnouncementCommand : IRequest<Unit>
{
    public Guid AnnouncementId { get; set; }
}

public class DeleteAnnouncementHandler : IRequestHandler<DeleteAnnouncementCommand, Unit>
{
    private readonly IAnnouncementRepository _repo;

    public DeleteAnnouncementHandler(IAnnouncementRepository repo)
    {
        _repo = repo;
    }

    public async Task<Unit> Handle(DeleteAnnouncementCommand request, CancellationToken ct)
    {
        var announcement = await _repo.GetByIdAsync(request.AnnouncementId);
        if (announcement != null)
        {
            _repo.Delete(announcement);
            await _repo.SaveChangesAsync();
        }

        return Unit.Value;
    }
}
