using Application.DTOs.NotificationDTOs;
using Application.UseCases.Notifications.Commands;
using Application.UseCases.Notifications.Commands.CreateNotification;
using Application.UseCases.Notifications.Commands.DeleteNotification;
using Application.UseCases.Notifications.Commands.MarkNotificationAsRead;
using Application.UseCases.Notifications.Queries;
using Application.UseCases.Notifications.Queries.GetAllNotificationsQuery;
using Application.UseCases.Notifications.Queries.GetNotificationsByUserQuery;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using WebAPI.Hubs;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/notification")]
    public class NotificationController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationController(
            IMediator mediator,
            IHubContext<NotificationHub> hub)
        {
            _mediator = mediator;
            _hub = hub;
        }

        // ==================================
        // CREATE
        // POST api/notification
        // ==================================
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateNotificationRequestDto request)
        {
            var command = new CreateNotificationCommand
            {
                UserId = request.UserId,
                Title = request.Title,
                Message = request.Message,
                Type = request.Type,
                ScopeType = request.ScopeType,
                ScopeId = request.ScopeId,
                CreatedBy = request.CreatedBy
            };

            var notification = await _mediator.Send(command);

            // SignalR realtime
            await _hub.Clients.User(request.UserId.ToString())
                .SendAsync("ReceiveNotification", notification);

            return Ok(notification);
        }


        // ==================================
        // GET BY USER
        // GET api/notification/user/{userId}
        // ==================================
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUser(Guid userId)
        {
            var list = await _mediator.Send(new GetNotificationsByUserQuery(userId));
            return Ok(list);
        }


        // ==================================
        // GET ALL (ADMIN)
        // GET api/notification/all
        // ==================================
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var list = await _mediator.Send(new GetAllNotificationsQuery());
            return Ok(list);
        }


        // ==================================
        // MARK AS READ
        // PUT api/notification/read/{id}
        // ==================================
        [HttpPut("read/{id}")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            try
            {
                await _mediator.Send(new MarkNotificationAsReadCommand(id));
                return Ok(new { message = "Marked as read" });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _mediator.Send(new DeleteNotificationCommand(id));
                return Ok("Deleted");
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

    }
}