using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Services;

namespace TaskFlowWebApplication.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/tasks")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }
        private bool TryGetCurrentUserId(out int userId)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out userId) && userId > 0;
        }
        private async Task<IActionResult> ExecuteAsync(Func<int, Task<IActionResult>> action)
        {
            if (!TryGetCurrentUserId(out int userId))
            {
                return Unauthorized(new
                {
                    message = "Thông tin đăng nhập không hợp lệ."
                });
            }
            try
            {
                return await action(userId);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (BusinessConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
        [HttpPost("lists/{listId:int:min(1)}")]
        public Task<IActionResult> CreateTask(int listId,[FromBody] CreateTaskDTO createTaskDTO)
        {
            return ExecuteAsync(async userId =>
            {
                var result = await _taskService.CreateTaskAsync(
                    listId, createTaskDTO, userId);

                return CreatedAtAction(nameof(GetTaskById),new 
                { 
                    taskId = result.Id 
                },result);
            });
        }
        [HttpGet("columns/{columnId:int:min(1)}")]
        public Task<IActionResult> GetTasksByColumn(int columnId)
        {
            return ExecuteAsync(async userId =>
            {
                var result = await _taskService.GetTasksByColumnAsync(columnId, userId);
                if (result == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy cột."
                    });
                }return Ok(result);
            });
        }
        [HttpGet("{taskId:int:min(1)}")]
        public Task<IActionResult> GetTaskById(int taskId)
        {
            return ExecuteAsync(async userId =>
            {
                var result = await _taskService.GetTaskByIdAsync(taskId, userId);
                if (result == null)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy công việc."
                    });
                }
                return Ok(result);
            });
        }
        [HttpPut("{taskId:int:min(1)}")]
        public Task<IActionResult> UpdateTask(int taskId,[FromBody] UpdateTaskDTO updateTaskDTO)
        {
            return ExecuteAsync(async userId =>
            {
                bool updated = await _taskService.UpdateTaskAsync(taskId, updateTaskDTO, userId);
                if (!updated)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy công việc."
                    });
                }
                return NoContent();
            });
        }

        [HttpDelete("{taskId:int:min(1)}")]
        public Task<IActionResult> DeleteTask(int taskId)
        {
            return ExecuteAsync(async userId =>
            {
                var result = await _taskService.DeleteTaskAsync(taskId, userId);

                switch (result)
                {
                    case DeleteTaskResult.Success:
                        return NoContent();

                    case DeleteTaskResult.NotFound:
                        return NotFound(new
                        {
                            message = "Không tìm thấy công việc."
                        });

                    case DeleteTaskResult.Forbidden:
                        return Forbid();

                    case DeleteTaskResult.HasDependencies:
                        return Conflict(new
                        {
                            message ="Công việc còn dữ liệu liên quan nên chưa thể xóa."
                        });

                    default:
                        throw new Exception("Kết quả xóa không xác định.");
                }
            });
        }

        [HttpPatch("{taskId:int:min(1)}/move")]
        public Task<IActionResult> MoveTask(int taskId,[FromBody] MoveTaskDTO moveTaskDTO)
        {
            return ExecuteAsync(async userId =>
            {
                bool moved = await _taskService.MoveTaskAsync(taskId, moveTaskDTO, userId);

                if (!moved)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy công việc."
                    });
                }
                return NoContent();
            });
        }

        [HttpPost("{taskId:int:min(1)}/assignees")]
        public Task<IActionResult> AssignTask(int taskId,[FromBody] AssignTaskDTO assignTaskDTO)
        {
            return ExecuteAsync(async userId =>
            {
                bool assigned = await _taskService.AssignTaskAsync(taskId, assignTaskDTO, userId);
                if (!assigned)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy công việc."
                    });
                }

                return NoContent();
            });
        }

        [HttpPatch("{taskId:int:min(1)}/status")]
        public Task<IActionResult> UpdateTaskStatus(int taskId,[FromBody] UpdateTaskStatusDTO updateTaskStatusDTO)
        {
            return ExecuteAsync(async userId =>
            {
                bool updated = await _taskService.UpdateTaskStatusAsync(taskId, updateTaskStatusDTO, userId);
                if (!updated)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy công việc."
                    });
                }
                return NoContent();
            });
        }

        [HttpPatch("{taskId:int:min(1)}/deadline")]
        public Task<IActionResult> UpdateTaskDeadline(int taskId,[FromBody] UpdateTaskDeadlineDTO updateTaskDeadlineDTO)
        {
            return ExecuteAsync(async userId =>
            {
                bool updated = await _taskService.UpdateTaskDeadlineAsync(taskId, updateTaskDeadlineDTO, userId);

                if (!updated)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy công việc."
                    });
                }
                return NoContent();
            });
        }

        [HttpPatch("lists/{listId:int:min(1)}/reorder")]
        public Task<IActionResult> ReorderTasks(int listId,[FromBody] ReorderTasksDTO reorderTasksDTO)
        {
            return ExecuteAsync(async userId =>
            {
                bool reordered = await _taskService.ReorderTasksAsync(listId, reorderTasksDTO, userId);
                if (!reordered)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy danh sách."
                    });
                }
                return NoContent();
            });
        }
        [HttpGet("~/api/workspaces/{workspaceId:int:min(1)}/tasks")]
        public Task<IActionResult> GetTasksByWorkspace(int workspaceId,[FromQuery] TaskFilterDTO filter)
        {
            return ExecuteAsync(async userId =>
            {
                var result = await _taskService.GetTasksByWorkspaceAsync(workspaceId, filter, userId);
                return Ok(result);
            });
        }
        [HttpGet("columns/{columnId:int:min(1)}/lists")]
        public Task<IActionResult> GetListsByColumn(int columnId)
        {
            return ExecuteAsync(async userId =>
            {
                var result = await _taskService.GetListsByColumnAsync(columnId, userId);
                return Ok(result);
            });
        }

        [HttpPatch("{taskId:int:min(1)}/move-column")]
        public Task<IActionResult> MoveTaskToColumn(int taskId, [FromBody] MoveTaskToColumnDTO moveTaskToColumnDTO)
        {
            return ExecuteAsync(async userId =>
            {
                bool moved = await _taskService.MoveTaskToColumnAsync(taskId, moveTaskToColumnDTO, userId);
                if (!moved)
                {
                    return NotFound(new
                    {
                        message = "Không tìm thấy công việc."
                    });
                }
                return NoContent();
            });
        }

        [HttpGet("me")]
        public Task<IActionResult> GetMyTasks([FromQuery] int workspaceId,[FromQuery] TaskFilterDTO filter)
        {
            return ExecuteAsync(async userId =>
            {
                if (workspaceId <= 0)
                {
                    return BadRequest(new
                    {
                        message = "workspaceId phải là số dương."
                    });
                }

                var result = await _taskService.GetMyTasksAsync(workspaceId, filter, userId);
                return Ok(result);
            });
        }

        [HttpPost("{taskId:int:min(1)}/comments")]
        public Task<IActionResult> AddComment(int taskId, [FromBody] SaveTaskCommentDTO saveTaskCommentDTO)
        {
            return ExecuteAsync(async userId =>
            {
                var result = await _taskService.AddCommentAsync(taskId, saveTaskCommentDTO, userId);
                return StatusCode(StatusCodes.Status201Created, result);
            });
        }

        [HttpGet("{taskId:int:min(1)}/comments")]
        public Task<IActionResult> GetComments(int taskId,[FromQuery] int page = 1,[FromQuery] int pageSize = 20)
        {
            return ExecuteAsync(async userId =>
            {
                var result = await _taskService.GetCommentsAsync(taskId, page, pageSize, userId);
                return Ok(result);
            });
        }

        [HttpPut("{taskId:int:min(1)}/comments/{commentId:int:min(1)}")]
        public Task<IActionResult> UpdateComment(int taskId, int commentId,[FromBody] SaveTaskCommentDTO saveTaskCommentDTO)
        {
            return ExecuteAsync(async userId =>
            {
                var result = await _taskService.UpdateCommentAsync(taskId, commentId, saveTaskCommentDTO, userId);
                return Ok(result);
            });
        }

        [HttpDelete("{taskId:int:min(1)}/comments/{commentId:int:min(1)}")]
        public Task<IActionResult> DeleteComment(int taskId, int commentId)
        {
            return ExecuteAsync(async userId =>
            {
                await _taskService.DeleteCommentAsync(taskId, commentId, userId);
                return NoContent();
            });
        }

        [HttpGet("{taskId:int:min(1)}/labels")]
        public Task<IActionResult> GetTaskLabels(int taskId)
        {
            return ExecuteAsync(async userId =>
            {
                var result = await _taskService.GetTaskLabelsAsync(taskId, userId);
                return Ok(result);
            });
        }

        [HttpPost("{taskId:int:min(1)}/labels/{labelId:int:min(1)}")]
        public Task<IActionResult> AddTaskLabel(int taskId, int labelId)
        {
            return ExecuteAsync(async userId =>
            {
                await _taskService.AddTaskLabelAsync(taskId, labelId, userId);
                return NoContent();
            });
        }
        [HttpDelete("{taskId:int:min(1)}/labels/{labelId:int:min(1)}")]
        public Task<IActionResult> RemoveTaskLabel(int taskId, int labelId)
        {
            return ExecuteAsync(async userId =>
            {
                await _taskService.RemoveTaskLabelAsync(taskId, labelId, userId);
                return NoContent();
            });
        }
        [HttpDelete("{taskId:int:min(1)}/assignees/{userId:int:min(1)}")]
        public Task<IActionResult> UnassignTask(int taskId, int userId)
        {
            return ExecuteAsync(async currentUserId =>
            {
                await _taskService.UnassignTaskAsync(taskId, userId, currentUserId);
                return NoContent();
            });
        }
        [HttpPut("comments/{commentId:int:min(1)}")]
        public Task<IActionResult> UpdateCommentById(int commentId, [FromBody] SaveTaskCommentDTO saveTaskCommentDTO)
        {
            return ExecuteAsync(async userId =>
            {
                var result = await _taskService.UpdateCommentByIdAsync(commentId, saveTaskCommentDTO, userId);
                return Ok(result);
            });
        }

        [HttpDelete("comments/{commentId:int:min(1)}")]
        public Task<IActionResult> DeleteCommentById(int commentId)
        {
            return ExecuteAsync(async userId =>
            {
                await _taskService.DeleteCommentByIdAsync(commentId, userId);
                return NoContent();
            });
        }

        [HttpGet("~/api/workspaces/{workspaceId:int:min(1)}/labels")]
        public Task<IActionResult> GetWorkspaceLabels(int workspaceId)
        {
            return ExecuteAsync(async userId =>
            {
                var result = await _taskService.GetWorkspaceLabelsAsync(workspaceId, userId);
                return Ok(result);
            });
        }

        [HttpPost("~/api/workspaces/{workspaceId:int:min(1)}/labels")]
        public Task<IActionResult> CreateWorkspaceLabel(int workspaceId, [FromBody] SaveWorkspaceLabelDTO saveWorkspaceLabelDTO)
        {
            return ExecuteAsync(async userId =>
            {
                var result = await _taskService.CreateWorkspaceLabelAsync(workspaceId, saveWorkspaceLabelDTO, userId);
                return StatusCode(StatusCodes.Status201Created, result);
            });
        }

        [HttpPut("~/api/workspaces/{workspaceId:int:min(1)}/labels/{labelId:int:min(1)}")]
        public Task<IActionResult> UpdateWorkspaceLabel(int workspaceId, int labelId, [FromBody] SaveWorkspaceLabelDTO saveWorkspaceLabelDTO)
        {
            return ExecuteAsync(async userId =>
            {
                var result = await _taskService.UpdateWorkspaceLabelAsync(workspaceId, labelId, saveWorkspaceLabelDTO, userId);
                return Ok(result);
            });
        }

        [HttpDelete("~/api/workspaces/{workspaceId:int:min(1)}/labels/{labelId:int:min(1)}")]
        public Task<IActionResult> DeleteWorkspaceLabel(int workspaceId, int labelId)
        {
            return ExecuteAsync(async userId =>
            {
                await _taskService.DeleteWorkspaceLabelAsync(workspaceId, labelId, userId);
                return NoContent();
            });
        }

    }
}
