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
        Task<List<TaskListResponseDTO>> GetListsByColumnAsync(int columnId, int userId);
        Task<bool> MoveTaskToColumnAsync(int taskId, MoveTaskToColumnDTO moveTaskToColumnDTO, int userId);
        Task<TaskPageResponseDTO> GetMyTasksAsync(int workspaceId, TaskFilterDTO filter, int userId);
        Task<TaskCommentResponseDTO> AddCommentAsync(int taskId, SaveTaskCommentDTO saveTaskCommentDTO, int userId);
        Task<List<TaskCommentResponseDTO>> GetCommentsAsync(int taskId, int page, int pageSize, int userId);
        Task<TaskCommentResponseDTO> UpdateCommentAsync(int taskId, int commentId,SaveTaskCommentDTO saveTaskCommentDTO, int userId);
        Task DeleteCommentAsync(int taskId, int commentId, int userId);
        Task<List<TaskLabelResponseDTO>> GetTaskLabelsAsync(int taskId, int userId);
        Task AddTaskLabelAsync(int taskId, int labelId, int userId);
        Task RemoveTaskLabelAsync(int taskId, int labelId, int userId);
        Task UnassignTaskAsync(int taskId, int assigneeUserId, int currentUserId);
        Task<TaskCommentResponseDTO> UpdateCommentByIdAsync(int commentId, SaveTaskCommentDTO saveTaskCommentDTO, int userId);
        Task DeleteCommentByIdAsync(int commentId, int userId);
        Task<List<TaskLabelResponseDTO>> GetWorkspaceLabelsAsync(int workspaceId, int userId);
        Task<TaskLabelResponseDTO> CreateWorkspaceLabelAsync(int workspaceId, SaveWorkspaceLabelDTO saveWorkspaceLabelDTO, int userId);
        Task<TaskLabelResponseDTO> UpdateWorkspaceLabelAsync(int workspaceId, int labelId, SaveWorkspaceLabelDTO saveWorkspaceLabelDTO, int userId);
        Task DeleteWorkspaceLabelAsync(int workspaceId, int labelId, int userId);
    }
}
