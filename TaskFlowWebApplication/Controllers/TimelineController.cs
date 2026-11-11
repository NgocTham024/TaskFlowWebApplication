
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.Data;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Models;

namespace TaskFlowWebApplication.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TimelineController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TimelineController(AppDbContext context)
        {
            _context = context;
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            var userIdValue = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(userIdValue, out userId);
        }

        // GET: api/timeline/upcoming?daysAhead=7
        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingTasks(
            [FromQuery] int daysAhead = 7)
        {
            if (!TryGetCurrentUserId(out int userId))
                return Unauthorized();

            if (daysAhead < 1 || daysAhead > 90)
            {
                return BadRequest(new
                {
                    message = "daysAhead must be between 1 and 90."
                });
            }

            var now = DateTime.UtcNow;
            var endDate = now.AddDays(daysAhead);

            var tasks = await _context.Tasks
                .AsNoTracking()
                .Where(t =>
                    t.DueDate.HasValue &&
                    t.DueDate.Value >= now &&
                    t.DueDate.Value <= endDate &&
                    t.CompletedAt == null &&
                    _context.WorkspaceMembers.Any(member =>
                        member.UserId == userId &&
                        member.WorkspaceId == t.List.Column.Board.WorkspaceId))
                .OrderBy(t => t.DueDate)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.StartDate,
                    t.DueDate,
                    t.StatusId,
                    t.PriorityId,
                    BoardId = t.List.Column.BoardId,
                    BoardName = t.List.Column.Board.Name,
                    WorkspaceId = t.List.Column.Board.WorkspaceId
                })
                .ToListAsync();

            return Ok(new
            {
                total = tasks.Count,
                daysAhead,
                tasks
            });
        }

        // GET: api/timeline/overdue
        [HttpGet("overdue")]
        public async Task<IActionResult> GetOverdueTasks()
        {
            if (!TryGetCurrentUserId(out int userId))
                return Unauthorized();

            var now = DateTime.UtcNow;

            var tasks = await _context.Tasks
                .AsNoTracking()
                .Where(t =>
                    t.DueDate.HasValue &&
                    t.DueDate.Value < now &&
                    t.CompletedAt == null &&
                    _context.WorkspaceMembers.Any(member =>
                        member.UserId == userId &&
                        member.WorkspaceId == t.List.Column.Board.WorkspaceId))
                .OrderBy(t => t.DueDate)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.StartDate,
                    t.DueDate,
                    t.StatusId,
                    t.PriorityId,
                    BoardId = t.List.Column.BoardId,
                    BoardName = t.List.Column.Board.Name,
                    WorkspaceId = t.List.Column.Board.WorkspaceId
                })
                .ToListAsync();

            return Ok(new
            {
                total = tasks.Count,
                tasks
            });
        }

        // PUT: api/timeline/tasks/5/schedule
        [HttpPut("tasks/{id:int}/schedule")]
        public async Task<IActionResult> UpdateTaskSchedule(
            int id,
            [FromBody] UpdateTaskScheduleDTO dto)
        {
            if (!TryGetCurrentUserId(out int userId))
                return Unauthorized();

            if (dto.StartDate.HasValue &&
                dto.DueDate.HasValue &&
                dto.StartDate.Value > dto.DueDate.Value)
            {
                return BadRequest(new
                {
                    message = "StartDate cannot be later than DueDate."
                });
            }

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound(new { message = "Task not found." });

            var workspaceId = await _context.Tasks
                .Where(t => t.Id == id)
                .Select(t => t.List.Column.Board.WorkspaceId)
                .FirstAsync();

            var isMember = await _context.WorkspaceMembers
                .AnyAsync(member =>
                    member.UserId == userId &&
                    member.WorkspaceId == workspaceId);

            if (!isMember)
                return Forbid();

            task.StartDate = dto.StartDate;
            task.DueDate = dto.DueDate;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Task schedule updated successfully.",
                taskId = task.Id,
                task.StartDate,
                task.DueDate,
                task.UpdatedAt
            });
        }
    }
}
