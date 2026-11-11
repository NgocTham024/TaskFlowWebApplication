
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.Data;

namespace TaskFlowWebApplication.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        // Get current logged-in user ID
        private bool TryGetCurrentUserId(out int userId)
        {
            var userIdValue = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(userIdValue, out userId);
        }

        // GET: api/notifications
        // Get all notifications of current user
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            if (!TryGetCurrentUserId(out int userId))
                return Unauthorized();

            var notifications = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Id,
                    n.UserId,
                    n.ActorId,
                    ActorName = n.Actor != null
                        ? n.Actor.Username
                        : null,
                    n.TaskId,
                    TaskTitle = n.Task != null
                        ? n.Task.Title
                        : null,
                    n.BoardId,
                    BoardName = n.Board != null
                        ? n.Board.Name
                        : null,
                    n.Type,
                    n.Message,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                total = notifications.Count,
                notifications
            });
        }

        // GET: api/notifications/unread
        // Get unread notifications of current user
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            if (!TryGetCurrentUserId(out int userId))
                return Unauthorized();

            var notifications = await _context.Notifications
                .AsNoTracking()
                .Where(n =>
                    n.UserId == userId &&
                    !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    n.Id,
                    n.UserId,
                    n.ActorId,
                    ActorName = n.Actor != null
                        ? n.Actor.Username
                        : null,
                    n.TaskId,
                    TaskTitle = n.Task != null
                        ? n.Task.Title
                        : null,
                    n.BoardId,
                    BoardName = n.Board != null
                        ? n.Board.Name
                        : null,
                    n.Type,
                    n.Message,
                    n.IsRead,
                    n.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                total = notifications.Count,
                notifications
            });
        }

        // PUT: api/notifications/{id}/read
        // Mark one notification as read
        [HttpPut("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            if (!TryGetCurrentUserId(out int userId))
                return Unauthorized();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.Id == id &&
                    n.UserId == userId);

            if (notification == null)
            {
                return NotFound(new
                {
                    message = "Notification not found."
                });
            }

            if (notification.IsRead)
            {
                return Ok(new
                {
                    message = "Notification is already read.",
                    notificationId = notification.Id,
                    isRead = true
                });
            }

            notification.IsRead = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Notification marked as read successfully.",
                notificationId = notification.Id,
                isRead = notification.IsRead
            });
        }

        // PUT: api/notifications/read-all
        // Mark all notifications as read
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            if (!TryGetCurrentUserId(out int userId))
                return Unauthorized();

            var unreadNotifications = await _context.Notifications
                .Where(n =>
                    n.UserId == userId &&
                    !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "All notifications marked as read successfully.",
                updatedCount = unreadNotifications.Count
            });
        }

        // DELETE: api/notifications/{id}
        // Delete one notification belonging to current user
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            if (!TryGetCurrentUserId(out int userId))
                return Unauthorized();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.Id == id &&
                    n.UserId == userId);

            if (notification == null)
            {
                return NotFound(new
                {
                    message = "Notification not found."
                });
            }

            _context.Notifications.Remove(notification);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Notification deleted successfully.",
                notificationId = id
            });
        }
    }
}
