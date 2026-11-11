using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Services;

namespace TaskFlowWebApplication.Controllers
{
    [ApiController]
    [Route("api/boards")]
    [Authorize]
    public class BoardController : ControllerBase
    {
        private readonly IBoardService _boardService;

        public BoardController(IBoardService boardService)
        {
            _boardService = boardService;
        }
        private bool TryGetUserId(out int userId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out userId);
        }
        [HttpPost("workspaces/{workspaceId:int}/boards")]
        public async Task<ActionResult<BoardResponseDTO>> CreateBoard(int workspaceId,[FromBody] CreateBoardDTO createBoardDTO)
        {
            if (!TryGetUserId(out int userId))
            {
                return Unauthorized();
            }
            try
            {
                var result = await _boardService.CreateBoardAsync(
                    workspaceId,
                    createBoardDTO,
                    userId);

                return CreatedAtAction(nameof(GetBoardById),new 
                { 
                    boardId = result.BoardId 
                }, result);
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
        [HttpGet("workspaces/{workspaceId:int}/boards")]
        public async Task<ActionResult<List<BoardResponseDTO>>> GetBoardsByWorkspace(int workspaceId)
        {
            if (!TryGetUserId(out int userId))
            {
                return Unauthorized();
            }
            try
            {
                var result = await _boardService.GetBoardsByWorkspaceAsync(workspaceId,userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
        [HttpGet("boards/{boardId:int}")]
        public async Task<ActionResult<BoardResponseDTO>> GetBoardById(int boardId)
        {
            if (!TryGetUserId(out int userId))
            {
                return Unauthorized();
            }
            try
            {
                var result = await _boardService.GetBoardByIdAsync(boardId,userId);
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
        [HttpPut("boards/{boardId:int}")]
        public async Task<ActionResult<BoardResponseDTO>> UpdateBoard(int boardId,[FromBody] UpdateBoardDTO updateBoardDTO)
        {
            if (!TryGetUserId(out int userId))
            {
                return Unauthorized();
            }
            try
            {
                var result = await _boardService.UpdateBoardAsync(boardId,updateBoardDTO,userId);
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
        [HttpDelete("boards/{boardId:int}")]
        public async Task<IActionResult> DeleteBoard(int boardId)
        {
            if (!TryGetUserId(out int userId))
            {
                return Unauthorized();
            }
            try
            {
                await _boardService.DeleteBoardAsync(boardId,userId);
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
    }
}
