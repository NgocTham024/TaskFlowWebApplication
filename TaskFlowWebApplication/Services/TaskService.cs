using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.Data;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Models;
using TaskEntity = TaskFlowWebApplication.Models.Tasks;


namespace TaskFlowWebApplication.Services
{
    public class TaskService: ITaskService
    {
        private readonly AppDbContext _context;

        public TaskService(AppDbContext context)
        {
            _context = context;
        }
        private async Task<bool> IsOwnerAsync(int workspaceId, int userId)
        {
            var membership = await _context.WorkspaceMembers.Include(wm => wm.Role).FirstOrDefaultAsync(wm =>wm.WorkspaceId == workspaceId &&wm.UserId == userId);
            return membership != null &&string.Equals(membership.Role.Name,"owner",StringComparison.OrdinalIgnoreCase);
        }
        private async Task<bool> IsMemberAsync(int workspaceId, int userId)
        {
            return await _context.WorkspaceMembers.AnyAsync(wm =>wm.WorkspaceId == workspaceId &&wm.UserId == userId);
        }
        private async Task<TaskEntity?> GetTaskWithWorkspaceAsync(int taskId)
        {
            return await _context.Tasks.Include(t => t.List).ThenInclude(l => l.Column).ThenInclude(c => c.Board).FirstOrDefaultAsync(t => t.Id == taskId);
        }
        private TaskResponseDTO MapTask(TaskEntity task)
        {
            return new TaskResponseDTO
            {
                Id = task.Id,
                ListId = task.ListId,
                ListName = task.List.Name,

                Title = task.Title,
                Description = task.Description,

                StatusId = task.StatusId,
                StatusName = task.Status.Name,

                PriorityId = task.PriorityId,
                PriorityName = task.Priority.Name,

                CreatedBy = task.CreatedBy,

                StartDate = task.StartDate,
                DueDate = task.DueDate,
                CompletedAt = task.CompletedAt,

                Position = task.Position,

                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,

                Assignees = task.TaskAssignees.Select(a => new TaskAssigneeResponseDTO
                {
                    UserId = a.UserId,
                    Username = a.User.Username
                }).ToList()
            };
        }
        public async Task<TaskResponseDTO> CreateTaskAsync(int listId, CreateTaskDTO createTaskDTO, int userId)
        {
            var list = await _context.TaskLists.Include(l => l.Column).ThenInclude(c => c.Board).FirstOrDefaultAsync(l => l.Id == listId);

            if (list == null)
            {
                throw new KeyNotFoundException("Không tìm thấy danh sách công việc.");
            }
            int workspaceId = list.Column.Board.WorkspaceId;

            if (!await IsOwnerAsync(workspaceId, userId))
            { 
                throw new UnauthorizedAccessException("Chỉ chủ sở hữu mới có thể tạo công việc."); 
            }

            if (createTaskDTO.StartDate.HasValue &&createTaskDTO.DueDate.HasValue &&createTaskDTO.DueDate < createTaskDTO.StartDate)
            {
                throw new ArgumentException("Ngày hết hạn không được sớm hơn ngày bắt đầu.");
            }
            if (string.IsNullOrWhiteSpace(createTaskDTO.Title))
            {
                throw new ArgumentException("Tiêu đề không được để trống.");
            }

            var todoStatus = await _context.TaskStatuses.AsNoTracking().SingleOrDefaultAsync(s => s.Name == "Todo");
            if (todoStatus == null)
            {
                throw new InvalidOperationException("Hệ thống chưa cấu hình trạng thái Todo.");
            }

            bool validPriority = await _context.TaskPriorities.AnyAsync(p => p.Id == createTaskDTO.PriorityId);

            if (!validPriority)
            {
                throw new ArgumentException("PriorityId không hợp lệ.");
            }
            int maxPosition = await _context.Tasks.Where(t => t.ListId == listId).Select(t => (int?)t.Position).MaxAsync() ?? -1;
            var taskEntity = new TaskEntity
            {
                ListId = listId,
                Title = createTaskDTO.Title.Trim(),
                Description = createTaskDTO.Description,

                StatusId = todoStatus.Id,
                PriorityId = createTaskDTO.PriorityId,
                CompletedAt = null,

                CreatedBy = userId,

                StartDate = createTaskDTO.StartDate,
                DueDate = createTaskDTO.DueDate,

                Position = maxPosition + 1,

                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Tasks.Add(taskEntity);
            await _context.SaveChangesAsync();

            var createdTask = await _context.Tasks
                .Include(t => t.List)
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.TaskAssignees)
                    .ThenInclude(a => a.User)
                .FirstAsync(t => t.Id == taskEntity.Id);
            return MapTask(createdTask);
        }
        public async Task<List<TaskResponseDTO>?> GetTasksByColumnAsync(int columnId, int userId)
        {
            var column = await _context.BoardColumns.Include(c => c.Board).FirstOrDefaultAsync(c => c.Id == columnId);
            if (column == null)
            {
                return null;
            }

            int workspaceId = column.Board.WorkspaceId;

            if (!await IsMemberAsync(workspaceId, userId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập vào không gian làm việc này.");
            }
            var tasks = await _context.Tasks
                .Where(t => t.List.ColumnId == columnId)
                .Include(t => t.List)
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.TaskAssignees)
                    .ThenInclude(a => a.User)
                .OrderBy(t => t.List.Position)
                .ThenBy(t => t.Position)
                .ToListAsync();
            return tasks.Select(MapTask).ToList();
        }
        public async Task<TaskDetailResponseDTO?>GetTaskByIdAsync(int taskId, int userId)
        {
            var task = await _context.Tasks
                .Include(t => t.List)
                    .ThenInclude(l => l.Column)
                        .ThenInclude(c => c.Board)
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.TaskAssignees)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
            { 
                return null;
            }

            int workspaceId = task.List.Column.Board.WorkspaceId;

            if (!await IsMemberAsync(workspaceId, userId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập vào tác vụ này.");
            }
            var response = new TaskDetailResponseDTO
            {
                Id = task.Id,
                ListId = task.ListId,
                ListName = task.List.Name,

                Title = task.Title,
                Description = task.Description,

                StatusId = task.StatusId,
                StatusName = task.Status.Name,

                PriorityId = task.PriorityId,
                PriorityName = task.Priority.Name,

                CreatedBy = task.CreatedBy,

                StartDate = task.StartDate,
                DueDate = task.DueDate,
                CompletedAt = task.CompletedAt,

                Position = task.Position,

                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,

                ColumnId = task.List.ColumnId,
                ColumnName = task.List.Column.Name,

                BoardId = task.List.Column.BoardId,
                BoardName = task.List.Column.Board.Name,

                Assignees = task.TaskAssignees.Select(a => new TaskAssigneeResponseDTO
                {
                    UserId = a.UserId,
                    Username = a.User.Username
                }).ToList()
            };
            return response;
        }
        public async Task<bool> UpdateTaskAsync(int taskId, UpdateTaskDTO updateTaskDTO, int userId)
        {
            var task = await GetTaskWithWorkspaceAsync(taskId);
            if (task == null)
            {
                return false;
            }

            int workspaceId = task.List.Column.Board.WorkspaceId;

            if (!await IsOwnerAsync(workspaceId, userId))
            {
                throw new UnauthorizedAccessException("Chỉ chủ sở hữu không gian làm việc mới có thể cập nhật các tác vụ.");
            }
            task.Title = updateTaskDTO.Title;
            task.Description = updateTaskDTO.Description;
            task.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        private async Task EnsureTaskOwnerAsync(TaskEntity task, int userId)
        {
            if (!await IsOwnerAsync(task.List.Column.Board.WorkspaceId, userId))
            {
                throw new UnauthorizedAccessException("Chỉ chủ sở hữu mới có thể quản lý công việc.");
            }
        }

        public async Task<DeleteTaskResult> DeleteTaskAsync(int taskId, int userId)
        {
            var task = await GetTaskWithWorkspaceAsync(taskId);
            if (task == null) 
            { 
                return DeleteTaskResult.NotFound; 
            }

            if (!await IsOwnerAsync(task.List.Column.Board.WorkspaceId, userId))
            {
                return DeleteTaskResult.Forbidden;
            }
            if (await _context.TaskAssignees.AnyAsync(a => a.TaskId == taskId)
                || await _context.TaskComments.AnyAsync(c => c.TaskId == taskId)
                || await _context.ActivityLogs.AnyAsync(a => a.TaskId == taskId)
                || await _context.Notifications.AnyAsync(n => n.TaskId == taskId)
                || await _context.Tasks.Where(t => t.Id == taskId).AnyAsync(t => t.Labels.Any()))
            {
                return DeleteTaskResult.HasDependencies;
            }
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return DeleteTaskResult.Success;
        }
        public async Task<bool> MoveTaskAsync(int taskId, MoveTaskDTO moveTaskDTO, int userId)
        {
            var task = await GetTaskWithWorkspaceAsync(taskId);
            if (task == null)
            {
                return false;
            } 
            await EnsureTaskOwnerAsync(task, userId);
            var target = await _context.TaskLists.Include(l => l.Column).ThenInclude(c => c.Board).FirstOrDefaultAsync(l => l.Id == moveTaskDTO.TargetListId);
            if (target == null)
            {
                throw new KeyNotFoundException("Không tìm thấy danh sách đích.");
            }
            if (target.Column.Board.WorkspaceId != task.List.Column.Board.WorkspaceId)
            {
                throw new ArgumentException("Danh sách đích phải thuộc cùng không gian làm việc.");
            }
            if (task.ListId == target.Id)
            {
                return true;
            }
            task.Position = (await _context.Tasks.Where(t => t.ListId == target.Id).Select(t => (int?)t.Position).MaxAsync() ?? -1) + 1;
            task.ListId = target.Id;
            task.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> AssignTaskAsync(int taskId, AssignTaskDTO assignTaskDTO, int userId)
        {
            var task = await GetTaskWithWorkspaceAsync(taskId);
            if (task == null)
            {
                return false;
            }
            await EnsureTaskOwnerAsync(task, userId);
            if (!await IsMemberAsync(task.List.Column.Board.WorkspaceId, assignTaskDTO.UserId))
            {
                throw new ArgumentException("Người được giao phải là thành viên của không gian làm việc.");
            }
            if (await _context.TaskAssignees.AnyAsync(a => a.TaskId == taskId && a.UserId == assignTaskDTO.UserId))
            {
                return true;
            }
            _context.TaskAssignees.Add(new TaskAssignee
            {
                TaskId = taskId, UserId = assignTaskDTO.UserId,
                AssignedBy = userId, AssignedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateTaskStatusAsync(int taskId, UpdateTaskStatusDTO updateTaskStatusDTO, int userId)
        {
            var task = await GetTaskWithWorkspaceAsync(taskId);
            if (task == null)
            {
                return false;
            }
            await EnsureTaskOwnerAsync(task, userId);

            var status = await _context.TaskStatuses.AsNoTracking().FirstOrDefaultAsync(s => s.Id == updateTaskStatusDTO.StatusId);
            if (status == null)
            {
                throw new ArgumentException("Trạng thái không tồn tại.");
            }

            bool isTodo = string.Equals(status.Name,"Todo",StringComparison.OrdinalIgnoreCase);
            bool isDoing = string.Equals(status.Name,"Doing",StringComparison.OrdinalIgnoreCase);
            bool isDone = string.Equals(status.Name,"Done",StringComparison.OrdinalIgnoreCase);
            if (!isTodo && !isDoing && !isDone)
            {
                throw new ArgumentException("Chỉ chấp nhận trạng thái Todo, Doing hoặc Done.");
            }
            var now = DateTime.UtcNow;
            task.StatusId = status.Id;
            if (isDone)
            {
                // Gửi lại Done không làm thay đổi thời điểm hoàn thành.
                task.CompletedAt ??= now;
            }
            else
            {
                // Khi mở lại công việc, bỏ thời điểm hoàn thành.
                task.CompletedAt = null;
            }
            task.UpdatedAt = now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateTaskDeadlineAsync(int taskId, UpdateTaskDeadlineDTO updateTaskDeadlineDTO, int userId)
        {
            var task = await GetTaskWithWorkspaceAsync(taskId);
            if (task == null) return false;
            await EnsureTaskOwnerAsync(task, userId);
            if (updateTaskDeadlineDTO.DueDate.HasValue && task.StartDate.HasValue && updateTaskDeadlineDTO.DueDate < task.StartDate)
            {
                throw new ArgumentException("Ngày hết hạn không được sớm hơn ngày bắt đầu.");
            }
            task.DueDate = updateTaskDeadlineDTO.DueDate;
            task.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReorderTasksAsync(int listId, ReorderTasksDTO reorderTasksDTO, int userId)
        {
            var list = await _context.TaskLists.Include(l => l.Column).ThenInclude(c => c.Board).FirstOrDefaultAsync(l => l.Id == listId);
            if (list == null) 
            { 
                return false; 
            }
            if (!await IsOwnerAsync(list.Column.Board.WorkspaceId, userId))
            {
                throw new UnauthorizedAccessException("Chỉ chủ sở hữu mới có thể sắp xếp công việc.");
            }
            var tasks = await _context.Tasks.Where(t => t.ListId == listId).ToListAsync();
            if (reorderTasksDTO.TaskIds == null || reorderTasksDTO.TaskIds.Count != tasks.Count|| reorderTasksDTO.TaskIds.Distinct().Count() != reorderTasksDTO.TaskIds.Count
                || !tasks.Select(t => t.Id).ToHashSet().SetEquals(reorderTasksDTO.TaskIds))
            {
                throw new ArgumentException("Phải cung cấp đủ ID công việc trong danh sách và không được trùng.");
            }
            if (tasks.Count == 0)
            {
                return true;
            }
            await using var transaction = await _context.Database.BeginTransactionAsync();
            int offset = checked(tasks.Max(t => t.Position) + tasks.Count + 1);
            foreach (var task in tasks)
            {
                task.Position = checked(task.Position + offset);
            }
            await _context.SaveChangesAsync();
            var byId = tasks.ToDictionary(t => t.Id);
            for (int i = 0; i < reorderTasksDTO.TaskIds.Count; i++)
            {
                byId[reorderTasksDTO.TaskIds[i]].Position = i;
                byId[reorderTasksDTO.TaskIds[i]].UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        public async Task<TaskPageResponseDTO> GetTasksByWorkspaceAsync(int workspaceId,TaskFilterDTO filter,int userId)
        {
            bool workspaceExists = await _context.Workspaces.AnyAsync(w => w.Id == workspaceId);
            if (!workspaceExists)
            {
                throw new KeyNotFoundException("Không tìm thấy không gian làm việc.");
            }

            if (!await IsMemberAsync(workspaceId, userId))
            {
                throw new UnauthorizedAccessException("Bạn không thuộc không gian làm việc này.");
            }

            if (filter.Page < 1 ||filter.PageSize < 1 ||filter.PageSize > 100)
            {
                throw new ArgumentException("Trang không hợp lệ.");
            }

            if (filter.DueFrom.HasValue &&filter.DueBefore.HasValue &&filter.DueFrom.Value >= filter.DueBefore.Value)
            {
                throw new ArgumentException("Thời gian bắt đầu phải trước thời gian kết thúc.");
            }

            if (filter.StatusId.HasValue)
            {
                var status = await _context.TaskStatuses.AsNoTracking().FirstOrDefaultAsync(s => s.Id == filter.StatusId.Value);

                if (status == null || !(string.Equals(status.Name, "Todo",StringComparison.OrdinalIgnoreCase) 
                    || string.Equals(status.Name, "Doing", StringComparison.OrdinalIgnoreCase) 
                    ||string.Equals(status.Name, "Done",StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException("Trạng thái không hợp lệ.");
                }
            }

            if (filter.AssigneeId.HasValue &&!await IsMemberAsync(workspaceId, filter.AssigneeId.Value))
            {
                throw new ArgumentException("Người phụ trách không thuộc không gian làm việc.");
            }
            var query = _context.Tasks.AsNoTracking().Where(t =>t.List.Column.Board.WorkspaceId == workspaceId);

            if (filter.StatusId.HasValue)
            {
                query = query.Where(t =>t.StatusId == filter.StatusId.Value);
            }

            if (filter.AssigneeId.HasValue)
            {
                query = query.Where(t =>t.TaskAssignees.Any(a =>a.UserId == filter.AssigneeId.Value));
            }

            if (filter.DueFrom.HasValue)
            {
                var fromUtc = filter.DueFrom.Value.UtcDateTime;

                query = query.Where(t =>t.DueDate.HasValue &&t.DueDate.Value >= fromUtc);
            }

            if (filter.DueBefore.HasValue)
            {
                var beforeUtc = filter.DueBefore.Value.UtcDateTime;

                query = query.Where(t =>t.DueDate.HasValue &&t.DueDate.Value < beforeUtc);
            }
            int totalCount = await query.CountAsync();
            long offset = ((long)filter.Page - 1) * filter.PageSize;
            var response = new TaskPageResponseDTO
            {
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };

            if (offset >= totalCount)
            {
                return response;
            }

            var tasks = await query
                .Include(t => t.List)
                .Include(t => t.Status)
                .Include(t => t.Priority)
                .Include(t => t.TaskAssignees)
                    .ThenInclude(a => a.User)
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .Skip((int)offset)
                .Take(filter.PageSize)
                .ToListAsync();

            response.Items = tasks.Select(MapTask).ToList();
            return response;
        }

    }
}
