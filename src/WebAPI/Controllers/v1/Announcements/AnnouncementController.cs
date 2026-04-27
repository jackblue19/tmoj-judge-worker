using Application.UseCases.Announcements.Commands.CreateAnnouncement;
using Application.UseCases.Announcements.Commands.DeleteAnnouncement;
using Application.UseCases.Announcements.Queries.GetActiveAnnouncements;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using System;
using System.Threading.Tasks;

namespace WebAPI.Controllers.v1.Announcements;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/announcements")]
[ApiController]
public class AnnouncementController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnnouncementController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ==================================
    // GET ACTIVE (PUBLIC)
    // GET api/v1/announcements
    // ==================================
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive()
    {
        var result = await _mediator.Send(new GetActiveAnnouncementsQuery());
        return Ok(result);
    }

    // ==================================
    // CREATE (ADMIN)
    // POST api/v1/announcements
    // ==================================
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { id, message = "Announcement created successfully" });
    }

    // ==================================
    // DELETE (ADMIN)
    // DELETE api/v1/announcements/{id}
    // ==================================
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteAnnouncementCommand { AnnouncementId = id });
        return Ok(new { message = "Announcement deleted successfully" });
    }
}
