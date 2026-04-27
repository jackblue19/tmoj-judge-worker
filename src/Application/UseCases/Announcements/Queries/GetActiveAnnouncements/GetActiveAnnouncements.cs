using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<GetActiveAnnouncementsHandler> _logger;

    public GetActiveAnnouncementsHandler(IAnnouncementRepository repo, ILogger<GetActiveAnnouncementsHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<List<AnnouncementDto>> Handle(GetActiveAnnouncementsQuery request, CancellationToken ct)
    {
        try 
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERROR fetching active announcements");
            throw;
        }
    }
}
