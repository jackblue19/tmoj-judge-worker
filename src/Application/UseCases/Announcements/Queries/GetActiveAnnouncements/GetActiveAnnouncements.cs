using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.UseCases.Announcements.Queries.GetActiveAnnouncements;

public class GetActiveAnnouncementsQuery : IRequest<List<AnnouncementDto>> { }

public class AnnouncementDto
{
    public Guid AnnouncementId { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool Pinned { get; set; }
    public string? ScopeType { get; set; }
    public Guid? ScopeId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GetActiveAnnouncementsHandler : IRequestHandler<GetActiveAnnouncementsQuery, List<AnnouncementDto>>
{
    private readonly IAnnouncementRepository _repo;

    public GetActiveAnnouncementsHandler(IAnnouncementRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<AnnouncementDto>> Handle(GetActiveAnnouncementsQuery request, CancellationToken ct)
    {
        var announcements = await _repo.GetActiveAnnouncementsAsync();
        
        var dtos = new List<AnnouncementDto>();
        foreach (var a in announcements)
        {
            dtos.Add(new AnnouncementDto
            {
                AnnouncementId = a.AnnouncementId,
                Title = a.Title,
                Content = a.Content,
                Pinned = a.Pinned,
                ScopeType = a.ScopeType,
                ScopeId = a.ScopeId,
                CreatedAt = a.CreatedAt
            });
        }

        return dtos;
    }
}
