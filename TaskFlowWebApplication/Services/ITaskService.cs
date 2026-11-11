using TaskFlowWebApplication.DTOs;

namespace TaskFlowWebApplication.Services
{
    public enum DeleteTaskResult
    {
        Success,
        NotFound,
        Forbidden,
        HasDependencies
    }
    public interface ITaskService
    {
        Task<TaskResponseDTO> CreateTaskAsync(int listId, CreateTaskDTO createTaskDTO, int userId);
        Task<List<TaskResponseDTO>?> GetTasksByColumnAsync(int columnId, int userId);
        Task<TaskDetailResponseDTO?> GetTaskByIdAsync(int taskId, int userId);
        Task<bool> UpdateTaskAsync(int taskId, UpdateTaskDTO updateTaskDTO, int userId);
        Task<DeleteTaskResult> DeleteTaskAsync(int taskId, int userId);
        Task<bool> MoveTaskAsync(int taskId, MoveTaskDTO moveTaskDTO, int userId);
        Task<bool> AssignTaskAsync(int taskId, AssignTaskDTO assignTaskDTO, int userId);
        Task<bool> UpdateTaskStatusAsync(int taskId, UpdateTaskStatusDTO updateTaskStatusDTO, int userId);
        Task<bool> UpdateTaskDeadlineAsync(int taskId, UpdateTaskDeadlineDTO updateTaskDeadlineDTO, int userId);
        Task<bool> ReorderTasksAsync(int listId, ReorderTasksDTO reorderTasksDTO, int userId);
        Task<TaskPageResponseDTO> GetTasksByWorkspaceAsync(int workspaceId,TaskFilterDTO filter,int userId);
    }
}
