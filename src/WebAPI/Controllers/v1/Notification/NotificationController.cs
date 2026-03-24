using Application.DTOs.NotificationDTOs;
using Domain.Entities;
using Infrastructure.Persistence.Scaffolded.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebAPI.Hubs;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/notification")]
    public class NotificationController : ControllerBase
    {
        private readonly TmojDbContext _context;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationController(
            TmojDbContext context,
            IHubContext<NotificationHub> hub)
        {
            _context = context;
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

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

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
            var list = await _context.Notifications
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return Ok(list);
        }


        // ==================================
        // GET ALL (ADMIN)
        // GET api/notification/all
        // ==================================
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Notifications
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return Ok(list);
        }


        // ==================================
        // MARK AS READ
        // PUT api/notification/read/{id}
        // ==================================
        [HttpPut("read/{id}")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(x => x.NotificationId == id);

            if (notification == null)
                return NotFound();

            notification.IsRead = true;

            await _context.SaveChangesAsync();

            return Ok(notification);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(x => x.NotificationId == id);

            if (notification == null)
                return NotFound();

            _context.Notifications.Remove(notification);

            await _context.SaveChangesAsync();

            return Ok("Deleted");
        }

    }
}