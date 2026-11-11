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
            catch (InvalidOperationException ex)
                when (ex is not ObjectDisposedException)
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
                bool updated = await _taskService.UpdateTaskDeadlineAsync(
                    taskId, updateTaskDeadlineDTO, userId);

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
    }
}
