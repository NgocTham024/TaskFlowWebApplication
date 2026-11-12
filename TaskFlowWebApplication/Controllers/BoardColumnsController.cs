using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Services;

namespace TaskFlowWebApplication.Controllers
{
    [ApiController]
    [Route("api/boardColumns")]
    [Authorize]
    public class BoardColumnsController : ControllerBase
    {
        private readonly IBoardColumnService _columnService;
        public BoardColumnsController(IBoardColumnService columnService)
        {
            _columnService = columnService;
        }
        private bool TryGetCurrentUserId(out int userId)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out userId);
        }
        [HttpPost("boards/{boardId:int}/columns")]
        public async Task<IActionResult> CreateColumn(int boardId, [FromBody] CreateColumnDTO createColumnDTO)
        {
            if (!TryGetCurrentUserId(out int userId))
            {
                return Unauthorized();
            }
            try
            {
                var result = await _columnService.CreateColumnAsync(boardId, createColumnDTO, userId);
                return CreatedAtAction(nameof(GetColumnsByBoard),new 
                { 
                    boardId 
                },result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new 
                { 
                    message = ex.Message 
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new 
                { 
                    message = ex.Message 
                });
            }
        }
        [HttpGet("boards/{boardId:int}/columns")]
        public async Task<IActionResult> GetColumnsByBoard(int boardId)
        {
            if (!TryGetCurrentUserId(out int userId))
            {
                return Unauthorized();
            }
            try
            {
                var result = await _columnService.GetColumnsAsync(boardId, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new 
                { 
                    message = ex.Message 
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
        [HttpPut("columns/{columnId:int}")]
        public async Task<IActionResult> UpdateColumn(int columnId, [FromBody] UpdateColumnDTO updateColumnDTO)
        {
            if (!TryGetCurrentUserId(out int userId))
            {
                return Unauthorized();
            }
            try
            {
                var result = await _columnService.UpdateColumnAsync(columnId, updateColumnDTO, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new 
                { 
                    message = ex.Message 
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new 
                { 
                    message = ex.Message 
                });
            }
        }
        [HttpDelete("columns/{columnId:int}")]
        public async Task<IActionResult> DeleteColumn(int columnId)
        {
            if (!TryGetCurrentUserId(out int userId))
            {
                return Unauthorized();
            }
            try
            {
                await _columnService.DeleteColumnAsync(columnId, userId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
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
        [HttpPatch("boards/{boardId:int}/columns/reorder")]
        public async Task<IActionResult> ReorderColumns(int boardId, [FromBody] ReorderColumnsDTO reorderColumnsDTO)
        {
            if (!TryGetCurrentUserId(out int userId))
            {
                return Unauthorized();
            }
            try
            {
                await _columnService.ReorderColumnsAsync(boardId, reorderColumnsDTO, userId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new 
                { 
                    message = ex.Message 
                });
            }
        }
    }
}
