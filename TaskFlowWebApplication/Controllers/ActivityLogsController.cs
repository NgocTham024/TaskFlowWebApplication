
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.Data;

namespace TaskFlowWebApplication.Controller
{
    [ApiController]
    [Route("api/activity-logs")]
    [Authorize]
    public class ActivityLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ActivityLogsController(AppDbContext context)
        {
            _context = context;
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            var value = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(value, out userId);
        }

        private bool IsValidPaging(int page, int pageSize)
        {
            return page >= 1 && pageSize >= 1 && pageSize <= 100;
        }

        // Check whether current user belongs to workspace
        private async Task<bool> IsWorkspaceMember(
            int userId,
            int workspaceId)
        {
            return await _context.WorkspaceMembers
                .AnyAsync(m =>
                    m.UserId == userId &&
                    m.WorkspaceId == workspaceId);
        }

        // Check whether current user is owner or admin
        private async Task<bool> IsWorkspaceAdmin(
            int userId,
            int workspaceId)
        {
            return await _context.WorkspaceMembers
                .AnyAsync(m =>
                    m.UserId == userId &&
                    m.WorkspaceId == workspaceId &&
                    (m.Role.Name == "owner" ||
                     m.Role.Name == "admin"));
        }

        // Common activity log response
        private IQueryable<object> BuildActivityQuery(
            int workspaceId)
        {
            return _context.ActivityLogs
                .AsNoTracking()
                .Where(a => a.WorkspaceId == workspaceId)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.WorkspaceId,
                    a.BoardId,
                    BoardName = a.Board != null
                        ? a.Board.Name
                        : null,
                    a.TaskId,
                    TaskTitle = a.Task != null
                        ? a.Task.Title
                        : null,
                    a.UserId,
                    Username = a.User != null
                        ? a.User.Username
                        : null,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    a.Description,
                    a.CreatedAt
                });
        }

        // GET: api/activity-logs/workspace/1
        [HttpGet("workspace/{workspaceId:int}")]
        public async Task<IActionResult> GetWorkspaceActivity(
            int workspaceId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!TryGetCurrentUserId(out int userId))
                return Unauthorized();

            if (!IsValidPaging(page, pageSize))
                return BadRequest(new
                {
                    message = "Invalid page or pageSize. Maximum pageSize is 100."
                });

            var isMember = await IsWorkspaceMember(
                userId, workspaceId);

            if (!isMember)
                return Forbid();

            var query = BuildActivityQuery(workspaceId);

            var total = await _context.ActivityLogs
                .CountAsync(a => a.WorkspaceId == workspaceId);

            var activities = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                workspaceId,
                total,
                page,
                pageSize,
                activities
            });
        }

        // GET: api/activity-logs/board/1
        [HttpGet("board/{boardId:int}")]
        public async Task<IActionResult> GetBoardActivity(
            int boardId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!TryGetCurrentUserId(out int userId))
                return Unauthorized();

            if (!IsValidPaging(page, pageSize))
                return BadRequest(new
                {
                    message = "Invalid page or pageSize."
                });

            var board = await _context.Boards
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
                return NotFound(new { message = "Board not found." });

            var isMember = await IsWorkspaceMember(
                userId, board.WorkspaceId);

            if (!isMember)
                return Forbid();

            var baseQuery = _context.ActivityLogs
                .AsNoTracking()
                .Where(a =>
                    a.WorkspaceId == board.WorkspaceId &&
                    a.BoardId == boardId);

            var total = await baseQuery.CountAsync();

            var activities = await baseQuery
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.WorkspaceId,
                    a.BoardId,
                    BoardName = a.Board != null
                        ? a.Board.Name
                        : null,
                    a.TaskId,
                    TaskTitle = a.Task != null
                        ? a.Task.Title
                        : null,
                    a.UserId,
                    Username = a.User != null
                        ? a.User.Username
                        : null,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    a.Description,
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                boardId,
                total,
                page,
                pageSize,
                activities
            });
        }

        // GET: api/activity-logs/task/1
        [HttpGet("task/{taskId:int}")]
        public async Task<IActionResult> GetTaskActivity(
            int taskId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!TryGetCurrentUserId(out int userId))
                return Unauthorized();

            if (!IsValidPaging(page, pageSize))
                return BadRequest(new
                {
                    message = "Invalid page or pageSize."
                });

            var taskInfo = await _context.Tasks
                .AsNoTracking()
                .Where(t => t.Id == taskId)
                .Select(t => new
                {
                    t.Id,
                    WorkspaceId = t.List.Column.Board.WorkspaceId
                })
                .FirstOrDefaultAsync();

            if (taskInfo == null)
                return NotFound(new { message = "Task not found." });

            var isMember = await IsWorkspaceMember(
                userId, taskInfo.WorkspaceId);

            if (!isMember)
                return Forbid();

            var baseQuery = _context.ActivityLogs
                .AsNoTracking()
                .Where(a =>
                    a.WorkspaceId == taskInfo.WorkspaceId &&
                    a.TaskId == taskId);

            var total = await baseQuery.CountAsync();

            var activities = await baseQuery
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.WorkspaceId,
                    a.BoardId,
                    BoardName = a.Board != null
                        ? a.Board.Name
                        : null,
                    a.TaskId,
                    TaskTitle = a.Task != null
                        ? a.Task.Title
                        : null,
                    a.UserId,
                    Username = a.User != null
                        ? a.User.Username
                        : null,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    a.Description,
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                taskId,
                total,
                page,
                pageSize,
                activities
            });
        }

        // GET: api/activity-logs/user/2?workspaceId=1
        // Only workspace owner and admin can view another user's activity
        [HttpGet("user/{targetUserId:int}")]
        public async Task<IActionResult> GetUserActivity(
            int targetUserId,
            [FromQuery] int workspaceId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!TryGetCurrentUserId(out int currentUserId))
                return Unauthorized();

            if (!IsValidPaging(page, pageSize))
                return BadRequest(new
                {
                    message = "Invalid page or pageSize."
                });

            if (workspaceId <= 0)
                return BadRequest(new
                {
                    message = "workspaceId is required."
                });

            var isAdmin = await IsWorkspaceAdmin(
                currentUserId, workspaceId);

            if (!isAdmin)
                return Forbid();

            var targetIsMember = await IsWorkspaceMember(
                targetUserId, workspaceId);

            if (!targetIsMember)
                return NotFound(new
                {
                    message = "User is not a member of this workspace."
                });

            var baseQuery = _context.ActivityLogs
                .AsNoTracking()
                .Where(a =>
                    a.WorkspaceId == workspaceId &&
                    a.UserId == targetUserId);

            var total = await baseQuery.CountAsync();

            var activities = await baseQuery
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new
                {
                    a.Id,
                    a.WorkspaceId,
                    a.BoardId,
                    BoardName = a.Board != null
                        ? a.Board.Name
                        : null,
                    a.TaskId,
                    TaskTitle = a.Task != null
                        ? a.Task.Title
                        : null,
                    a.UserId,
                    Username = a.User != null
                        ? a.User.Username
                        : null,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    a.Description,
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                targetUserId,
                workspaceId,
                total,
                page,
                pageSize,
                activities
            });
        }
    }
}
