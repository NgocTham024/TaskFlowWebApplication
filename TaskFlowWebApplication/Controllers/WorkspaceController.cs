using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Services;

namespace TaskFlowWebApplication.Controllers
{
    [ApiController]
    [Route("api/workspaces")]
    [Authorize]
    public class WorkspaceController : ControllerBase
    {
        private readonly IWorkspaceService _workspaceService;
        public WorkspaceController(IWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }
        private bool TryGetCurrentUserId(out int userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out userId);
        }
        [HttpPost]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceDTO createWorkspaceDTO)
        {
            if (!TryGetCurrentUserId(out int userId))
            {
                return Unauthorized();
            }
            try
            {
                var workspace = await _workspaceService.CreateWorkspaceAsync(createWorkspaceDTO, userId);
                return CreatedAtAction(nameof(GetWorkspaceById),new
                    {
                        id = workspace.WorkspaceId
                    }, workspace
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new 
                { 
                    message = ex.Message 
                });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetMyWorkspaces()
        {
            if (!TryGetCurrentUserId(out int userId))return Unauthorized();
            var workspaces = await _workspaceService.GetWorkspacesAsync(userId);
            return Ok(workspaces);
        }
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetWorkspaceById(int id)
        {
            if (!TryGetCurrentUserId(out int userId))
            { 
                return Unauthorized(); 
            }
            try
            {
                var workspace = await _workspaceService.GetWorkspacesByIdAsync(id, userId);
                if (workspace == null)
                {
                    return NotFound(new { message = "Không tìm thấy không gian làm việc. " });
                }

                return Ok(workspace);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateWorkspace(int id,[FromBody] UpdateWorkspaceDTO dto)
        {
            if (!TryGetCurrentUserId(out int userId))
            {
                return Unauthorized();
            }
            try
            {
                bool updated = await _workspaceService.UpdateWorkspaceAsync(id, dto, userId);
                if (!updated)
                {
                    return NotFound(new { message = "Không tìm thấy không gian làm việc." });
                }

                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteWorkspace(int id)
        {
            if (!TryGetCurrentUserId(out int userId))
            {
                return Unauthorized();
            }
            try
            {
                bool deleted = await _workspaceService.DeleteWorkspaceAsync(id, userId);
                if (!deleted)
                {
                    return NotFound(new { message = "Không tìm thấy không gian làm việc." });
                }
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
        [HttpPost("{id:int}/members")]
        public async Task<IActionResult> AddMember(int id,[FromBody] AddWorkspaceMemberDTO dto)
        {
            if (!TryGetCurrentUserId(out int userId))
            {
                return Unauthorized();
            }
            try
            {
                bool added = await _workspaceService.AddMemberAsync(id, dto, userId);
                if (!added)
                {
                    return Conflict(new { message = "Unable to add member." });
                }
                return Ok(new { message = "Member added successfully." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("{id:int}/members")]
        public async Task<IActionResult> GetMembers(int id)
        {
            if (!TryGetCurrentUserId(out int userId))
            {
                return Unauthorized();
            }
            try
            {
                var members = await _workspaceService.GetMembersAsync(id, userId);
                if (members == null)
                {
                    return NotFound(new { message = "Workspace not found." });
                }
                return Ok(members);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
        [HttpDelete("{id:int}/members/{userId:int}")]
        public async Task<IActionResult> RemoveMember(int id,int userId)
        {
            if (!TryGetCurrentUserId(out int currentUserId))
            {
                return Unauthorized();
            }    
            try
            {
                bool removed = await _workspaceService.RemoveMemberAsync(id, userId, currentUserId);
                if (!removed)
                {
                    return NotFound(new { message = "Member not found." });
                }
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}
