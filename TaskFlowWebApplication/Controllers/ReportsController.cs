
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.Data;

namespace TaskFlowWebApplication.Controller
{
    [ApiController]
    [Route("api/reports")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // Get logged-in user ID
        private bool TryGetCurrentUserId(out int userId)
        {
            var value = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(value, out userId);
        }

        // Check workspace membership
        private Task<bool> IsWorkspaceMember(
            int userId,
            int workspaceId)
        {
            return _context.WorkspaceMembers.AnyAsync(m =>
                m.UserId == userId &&
                m.WorkspaceId == workspaceId);
        }

        // Check workspace existence and membership
        private async Task<bool> CanAccessWorkspace(
            int userId,
            int workspaceId)
        {
            return await IsWorkspaceMember(userId, workspaceId);
        }

        // Common workspace task query
        private IQueryable<Models.Tasks> GetWorkspaceTasks(
            int workspaceId)
        {
            return _context.Tasks.Where(t =>
                t.List.Column.Board.WorkspaceId == workspaceId);
        }

        // GET: api/reports/workspace/1/summary
        // Total tasks and tasks grouped by status
        [HttpGet("workspace/{workspaceId:int}/summary")]
        public async Task<IActionResult> GetSummary(
            int workspaceId)
        {
            if (!TryGetCurrentUserId(out int userId))
                return Unauthorized();

            if (!await CanAccessWorkspace(userId, workspaceId))
                return Forbid();

            var tasks = GetWorkspaceTasks(workspaceId);

            var totalTasks = await tasks.CountAsync();

            var completedTasks = await tasks.CountAsync(
                t => t.CompletedAt != null);

            var statusStatistics = await tasks
                .GroupBy(t => new
                {
                    t.StatusId,
                    StatusName = t.Status.Name
                })
                .Select(g => new
                {
                    statusId = g.Key.StatusId,
                    statusName = g.Key.StatusName,
                    total = g.Count()
                })
                .OrderBy(s => s.statusId)
                .ToListAsync();

            return Ok(new
            {
                workspaceId,
                totalTasks,
                completedTasks,
                incompleteTasks = totalTasks - completedTasks,
                statusStatistics
            });
        }

        // GET: api/reports/workspace/1/completion
        // Calculate completion percentage
        [HttpGet("workspace/{workspaceId:int}/completion")]
        public async Task<IActionResult> GetCompletion(
            int workspaceId)
        {
            if (!TryGetCurrentUserId(out int userId))
                return Unauthorized();

            if (!await CanAccessWorkspace(userId, workspaceId))
                return Forbid();

            var tasks = GetWorkspaceTasks(workspaceId);

            var totalTasks = await tasks.CountAsync();

            var completedTasks = await tasks.CountAsync(
                t => t.CompletedAt != null);

            var completionPercentage = totalTasks == 0
                ? 0
                : Math.Round(
                    (double)completedTasks / totalTasks * 100,
                    2);

            return Ok(new
            {
                workspaceId,
                totalTasks,
                completedTasks,
                incompleteTasks = totalTasks - completedTasks,
                completionPercentage
            });
        }

        // GET: api/reports/workspace/1/overdue
        // Count overdue tasks
        [HttpGet("workspace/{workspaceId:int}/overdue")]
        public async Task<IActionResult> GetOverdueTasks(
            int workspaceId)
        {
            if (!TryGetCurrentUserId(out int userId))
                return Unauthorized();

            if (!await CanAccessWorkspace(userId, workspaceId))
                return Forbid();

            var now = DateTime.UtcNow;

            var overdueTasks = await GetWorkspaceTasks(workspaceId)
                .Where(t =>
                    t.DueDate.HasValue &&
                    t.DueDate.Value < now &&
                    t.CompletedAt == null)
                .OrderBy(t => t.DueDate)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.DueDate,
                    t.StatusId,
                    StatusName = t.Status.Name,
                    t.PriorityId,
                    PriorityName = t.Priority.Name
                })
                .ToListAsync();

            return Ok(new
            {
                workspaceId,
                overdueCount = overdueTasks.Count,
                overdueTasks
            });
        }

        // GET: api/reports/workspace/1/members
        // Workload statistics by workspace member
        [HttpGet("workspace/{workspaceId:int}/members")]
        public async Task<IActionResult> GetMemberWorkload(
            int workspaceId)
        {
            if (!TryGetCurrentUserId(out int userId))
                return Unauthorized();

            if (!await CanAccessWorkspace(userId, workspaceId))
                return Forbid();

            var members = await _context.WorkspaceMembers
                .AsNoTracking()
                .Where(m => m.WorkspaceId == workspaceId)
                .Select(m => new
                {
                    userId = m.UserId,
                    username = m.User.Username,
                    role = m.Role.Name,

                    assignedTasks = _context.TaskAssignees.Count(a =>
                        a.UserId == m.UserId &&
                        a.Task.List.Column.Board.WorkspaceId
                            == workspaceId),

                    completedTasks = _context.TaskAssignees.Count(a =>
                        a.UserId == m.UserId &&
                        a.Task.List.Column.Board.WorkspaceId
                            == workspaceId &&
                        a.Task.CompletedAt != null),

                    incompleteTasks = _context.TaskAssignees.Count(a =>
                        a.UserId == m.UserId &&
                        a.Task.List.Column.Board.WorkspaceId
                            == workspaceId &&
                        a.Task.CompletedAt == null)
                })
                .OrderByDescending(m => m.assignedTasks)
                .ToListAsync();

            return Ok(new
            {
                workspaceId,
                totalMembers = members.Count,
                members
            });
        }

        // GET:
        // api/reports/workspace/1/period
        // ?fromDate=2026-11-01&toDate=2026-11-30
        [HttpGet("workspace/{workspaceId:int}/period")]
        public async Task<IActionResult> GetPeriodReport(
            int workspaceId,
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
        {
            if (!TryGetCurrentUserId(out int userId))
                return Unauthorized();

            if (!await CanAccessWorkspace(userId, workspaceId))
                return Forbid();

            if (fromDate == default || toDate == default)
            {
                return BadRequest(new
                {
                    message = "fromDate and toDate are required."
                });
            }

            if (fromDate.Date > toDate.Date)
            {
                return BadRequest(new
                {
                    message = "fromDate cannot be later than toDate."
                });
            }

            var start = fromDate.Date;
            var endExclusive = toDate.Date.AddDays(1);

            var tasks = GetWorkspaceTasks(workspaceId);

            // Tasks created during the selected period
            var createdTasks = await tasks.CountAsync(t =>
                t.CreatedAt >= start &&
                t.CreatedAt < endExclusive);

            // Tasks completed during the selected period
            var completedTasks = await tasks.CountAsync(t =>
                t.CompletedAt.HasValue &&
                t.CompletedAt.Value >= start &&
                t.CompletedAt.Value < endExclusive);

            // Tasks overdue at the end of the selected period
            var overdueTasks = await tasks.CountAsync(t =>
                t.DueDate.HasValue &&
                t.DueDate.Value < endExclusive &&
                t.CompletedAt == null);

            return Ok(new
            {
                workspaceId,
                fromDate = start,
                toDate = toDate.Date,
                createdTasks,
                completedTasks,
                overdueTasks
            });
        }
    }
}
