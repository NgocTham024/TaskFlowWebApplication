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
        private async Task<TaskEntity> RequireTaskMemberAsync(int taskId, int userId)
        {
            var task = await GetTaskWithWorkspaceAsync(taskId);
            if (task == null)
            {
                throw new KeyNotFoundException("Không tìm thấy công việc.");
            }

            int workspaceId = task.List.Column.Board.WorkspaceId;

            if (!await IsMemberAsync(workspaceId, userId))
            {
                throw new UnauthorizedAccessException("Bạn không thuộc không gian làm việc này.");
            }
            return task;
        }
        private static string ValidateCommentContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Nội dung bình luận không được để trống.");
            }
            return content.Trim();
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
            response.Comments = await _context.TaskComments
                .AsNoTracking()
                .Where(c => c.TaskId == taskId)
                .OrderBy(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .Select(c => new TaskCommentResponseDTO
                {
                    Id = c.Id,
                    TaskId = c.TaskId,
                    UserId = c.UserId,
                    Username = c.User.Username,
                    Content = c.ContentCmt,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

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
                // gửi lại done không làm thay đổi thời điểm hoàn thành.
                task.CompletedAt ??= now;
            }
            else
            {
                // khi mở lại công việc, bỏ thời điểm hoàn thành.
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
            if (filter.PriorityId.HasValue)
            {
                bool exists = await _context.TaskPriorities.AnyAsync(p => p.Id == filter.PriorityId.Value);
                if (!exists)
                {
                    throw new ArgumentException("Mức ưu tiên không tồn tại.");
                }
                query = query.Where(t =>t.PriorityId == filter.PriorityId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var search = filter.Search.Trim();
                query = query.Where(t =>t.Title.Contains(search) ||(t.Description != null &&t.Description.Contains(search)));
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
        public async Task<List<TaskListResponseDTO>> GetListsByColumnAsync(int columnId, int userId)
        {
            var column = await _context.BoardColumns.AsNoTracking().Include(c => c.Board).FirstOrDefaultAsync(c => c.Id == columnId);
            if (column == null)
            {
                throw new KeyNotFoundException("Không tìm thấy cột.");
            }
            if (!await IsMemberAsync(column.Board.WorkspaceId, userId))
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xem cột này.");
            }
            return await _context.TaskLists
                .AsNoTracking()
                .Where(l => l.ColumnId == columnId)
                .OrderBy(l => l.Position)
                .ThenBy(l => l.Id)
                .Select(l => new TaskListResponseDTO
                {
                    Id = l.Id,
                    ColumnId = l.ColumnId,
                    Name = l.Name,
                    Position = l.Position
                }).ToListAsync();
        }
        public async Task<bool> MoveTaskToColumnAsync(int taskId, MoveTaskToColumnDTO moveTaskToColumnDTO, int userId)
        {
            var task = await GetTaskWithWorkspaceAsync(taskId);

            if (task == null)
            {
                return false;
            }
            await EnsureTaskOwnerAsync(task, userId);
            int workspaceId = task.List.Column.Board.WorkspaceId;
            bool validColumn = await _context.BoardColumns.AnyAsync(c =>c.Id == moveTaskToColumnDTO.TargetColumnId &&c.Board.WorkspaceId == workspaceId);

            if (!validColumn)
            {
                throw new ArgumentException("Cột đích không tồn tại trong workspace của task.");
            }
            var lists = _context.TaskLists.Where(l => l.ColumnId == moveTaskToColumnDTO.TargetColumnId);
            int? targetListId;
            if (moveTaskToColumnDTO.TargetListId.HasValue)
            {
                targetListId = await lists
                    .Where(l => l.Id == moveTaskToColumnDTO.TargetListId.Value)
                    .Select(l => (int?)l.Id)
                    .FirstOrDefaultAsync();
                if (!targetListId.HasValue)
                {
                    throw new ArgumentException("List đích không thuộc cột đích.");
                }
            }
            else
            {
                targetListId = await lists
                    .OrderBy(l => l.Position)
                    .ThenBy(l => l.Id)
                    .Select(l => (int?)l.Id)
                    .FirstOrDefaultAsync();
                if (!targetListId.HasValue)
                {
                    throw new BusinessConflictException("Cột đích chưa có list để chứa task.");
                }
            }

            return await MoveTaskAsync(taskId,new MoveTaskDTO
            {
                TargetListId = targetListId.Value
            },userId);
        }
        public async Task<TaskPageResponseDTO> GetMyTasksAsync(int workspaceId, TaskFilterDTO filter, int userId)
        {
            // luôn lấy người hiện tại từ jwt, bỏ qua assigneeid đầu vào.
            filter.AssigneeId = userId;
            return await GetTasksByWorkspaceAsync(workspaceId, filter, userId);
        }
        private static TaskCommentResponseDTO MapComment(TaskComment comment, string username)
        {
            return new TaskCommentResponseDTO
            {
                Id = comment.Id,
                TaskId = comment.TaskId,
                UserId = comment.UserId,
                Username = username,
                Content = comment.ContentCmt,
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt
            };
        }
        public async Task<TaskCommentResponseDTO> AddCommentAsync(int taskId, SaveTaskCommentDTO dto, int userId)
        {
            await RequireTaskMemberAsync(taskId, userId);
            var content = ValidateCommentContent(dto.Content);
            var username = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.Username)
                .FirstAsync();

            var comment = new TaskComment
            {
                TaskId = taskId,
                UserId = userId,
                ContentCmt = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.TaskComments.Add(comment);
            await _context.SaveChangesAsync();

            return MapComment(comment, username);
        }
        public async Task<List<TaskCommentResponseDTO>> GetCommentsAsync(int taskId, int page, int pageSize, int userId)
        {
            await RequireTaskMemberAsync(taskId, userId);
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                throw new ArgumentException("Trang không hợp lệ.");
            }
            long offset = ((long)page - 1) * pageSize;
            if (offset > int.MaxValue)
            {
                throw new ArgumentException("Trang quá lớn.");
            }

            return await _context.TaskComments
                .AsNoTracking()
                .Where(c => c.TaskId == taskId)
                .OrderBy(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .Skip((int)offset)
                .Take(pageSize)
                .Select(c => new TaskCommentResponseDTO
                {
                    Id = c.Id,
                    TaskId = c.TaskId,
                    UserId = c.UserId,
                    Username = c.User.Username,
                    Content = c.ContentCmt,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToListAsync();
        }
        public async Task<TaskCommentResponseDTO> UpdateCommentAsync(int taskId, int commentId,SaveTaskCommentDTO saveTaskCommentDTO, int userId)
        {
            await RequireTaskMemberAsync(taskId, userId);
            var comment = await _context.TaskComments.Include(c => c.User).FirstOrDefaultAsync(c =>c.Id == commentId && c.TaskId == taskId);

            if (comment == null)
            {
                throw new KeyNotFoundException("Không tìm thấy bình luận.");
            }

            if (comment.UserId != userId)
            {
                throw new UnauthorizedAccessException("Bạn chỉ được sửa bình luận của mình.");
            }

            comment.ContentCmt = ValidateCommentContent(saveTaskCommentDTO.Content);
            comment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapComment(comment, comment.User.Username);
        }

        public async Task DeleteCommentAsync(int taskId, int commentId, int userId)
        {
            var task = await RequireTaskMemberAsync(taskId, userId);
            var comment = await _context.TaskComments.FirstOrDefaultAsync(c =>c.Id == commentId && c.TaskId == taskId);

            if (comment == null)
            {
                throw new KeyNotFoundException("Không tìm thấy bình luận.");
            }
            bool isOwner = await IsOwnerAsync(task.List.Column.Board.WorkspaceId, userId);
            if (comment.UserId != userId && !isOwner)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xóa bình luận này.");
            }
            _context.TaskComments.Remove(comment);
            await _context.SaveChangesAsync();
        }
        public async Task<List<TaskLabelResponseDTO>> GetTaskLabelsAsync(int taskId, int userId)
        {
            await RequireTaskMemberAsync(taskId, userId);

            return await _context.Tasks
                .AsNoTracking()
                .Where(t => t.Id == taskId)
                .SelectMany(t => t.Labels)
                .OrderBy(l => l.Name)
                .Select(l => new TaskLabelResponseDTO
                {
                    Id = l.Id,
                    Name = l.Name,
                    Color = l.Color
                })
                .ToListAsync();
        }
        public async Task AddTaskLabelAsync(int taskId, int labelId, int userId)
        {
            var task = await RequireTaskMemberAsync(taskId, userId);
            await EnsureTaskOwnerAsync(task, userId);

            int workspaceId = task.List.Column.Board.WorkspaceId;

            var label = await _context.Labels.FirstOrDefaultAsync(l =>l.Id == labelId && l.WorkspaceId == workspaceId);

            if (label == null)
            {
                throw new ArgumentException("Nhãn không tồn tại trong workspace của task.");
            }

            await _context.Entry(task).Collection(t => t.Labels).LoadAsync();

            if (task.Labels.Any(l => l.Id == labelId))
            {
                return;
            }

            task.Labels.Add(label);
            await _context.SaveChangesAsync();
        }
        public async Task RemoveTaskLabelAsync(int taskId, int labelId, int userId)
        {
            var task = await RequireTaskMemberAsync(taskId, userId);
            await EnsureTaskOwnerAsync(task, userId);
            await _context.Entry(task).Collection(t => t.Labels).LoadAsync();

            var label = task.Labels.FirstOrDefault(l => l.Id == labelId);

            if (label == null)
            {
                return;
            }
            task.Labels.Remove(label);
            await _context.SaveChangesAsync();
        }
        public async Task UnassignTaskAsync(int taskId, int assigneeUserId, int currentUserId)
        {
            var task = await RequireTaskMemberAsync(taskId, currentUserId);

            await EnsureTaskOwnerAsync(task, currentUserId);

            var assignment = await _context.TaskAssignees.FirstOrDefaultAsync(a =>a.TaskId == taskId &&a.UserId == assigneeUserId);

            // đã hủy trước đó thì vẫn coi là thành công.
            if (assignment == null)
            {
                return;
            }
            _context.TaskAssignees.Remove(assignment);
            await _context.SaveChangesAsync();
        }
        private async Task<int> GetCommentTaskIdAsync(int commentId)
        {
            var comment = await _context.TaskComments.AsNoTracking().FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                throw new KeyNotFoundException("Không tìm thấy bình luận.");
            }
            return comment.TaskId;
        }

        public async Task<TaskCommentResponseDTO> UpdateCommentByIdAsync(int commentId, SaveTaskCommentDTO saveTaskCommentDTO, int userId)
        {
            int taskId = await GetCommentTaskIdAsync(commentId);
            var result = await UpdateCommentAsync(taskId, commentId, saveTaskCommentDTO, userId);
            return result;
        }

        public async Task DeleteCommentByIdAsync(int commentId, int userId)
        {
            int taskId = await GetCommentTaskIdAsync(commentId);
            await DeleteCommentAsync(taskId, commentId, userId);
        }

        private async Task RequireWorkspaceAccessAsync(int workspaceId, int userId, bool ownerOnly)
        {
            bool workspaceExists = await _context.Workspaces.AnyAsync(w => w.Id == workspaceId);

            if (!workspaceExists)
            {
                throw new KeyNotFoundException("Không tìm thấy không gian làm việc.");
            }
            bool allowed;
            if (ownerOnly)
            {
                allowed = await IsOwnerAsync(workspaceId, userId);
            }
            else
            {
                allowed = await IsMemberAsync(workspaceId, userId);
            }
            if (!allowed)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền thực hiện thao tác này.");
            }
        }
       
        private static TaskLabelResponseDTO MapWorkspaceLabel(Label label)
        {
            var response = new TaskLabelResponseDTO
            {
                Id = label.Id,
                Name = label.Name,
                Color = label.Color
            };

            return response;
        }

        private async Task SaveWorkspaceLabelChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var sqlException = ex.GetBaseException() as Microsoft.Data.SqlClient.SqlException;
                if (sqlException != null)
                {
                    if (sqlException.Number == 2601 || sqlException.Number == 2627)
                    {
                        throw new BusinessConflictException("Tên nhãn đã tồn tại hoặc workspace đã có nhãn.");
                    }
                }

                throw;
            }
        }

        public async Task<List<TaskLabelResponseDTO>> GetWorkspaceLabelsAsync(int workspaceId, int userId)
        {
            await RequireWorkspaceAccessAsync(workspaceId, userId, false);

            var labels = await _context.Labels
                .AsNoTracking()
                .Where(l => l.WorkspaceId == workspaceId)
                .OrderBy(l => l.Name)
                .Select(l => new TaskLabelResponseDTO
                {
                    Id = l.Id,
                    Name = l.Name,
                    Color = l.Color
                }).ToListAsync();

            return labels;
        }

        public async Task<TaskLabelResponseDTO> CreateWorkspaceLabelAsync(int workspaceId, SaveWorkspaceLabelDTO saveWorkspaceLabelDTO, int userId)
        {
            await RequireWorkspaceAccessAsync(workspaceId, userId, true);
            string name = saveWorkspaceLabelDTO.Name.Trim();
            bool workspaceHasLabel = await _context.Labels.AnyAsync(l => l.WorkspaceId == workspaceId);
            if (workspaceHasLabel)
            {
                throw new BusinessConflictException("Cấu trúc hiện tại chỉ cho phép một nhãn trên mỗi workspace.");
            }

            bool nameExists = await _context.Labels.AnyAsync(l => l.Name == name);
            if (nameExists)
            {
                throw new BusinessConflictException("Tên nhãn đã tồn tại.");
            }

            var label = new Label
            {
                WorkspaceId = workspaceId,
                Name = name,
                Color = saveWorkspaceLabelDTO.Color.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Labels.Add(label);
            await SaveWorkspaceLabelChangesAsync();

            var response = MapWorkspaceLabel(label);
            return response;
        }
        public async Task<TaskLabelResponseDTO> UpdateWorkspaceLabelAsync(int workspaceId, int labelId, SaveWorkspaceLabelDTO dto, int userId)
        {
            await RequireWorkspaceAccessAsync(workspaceId, userId, true);
            var label = await _context.Labels.FirstOrDefaultAsync(l =>l.Id == labelId && l.WorkspaceId == workspaceId);

            if (label == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhãn trong workspace.");
            }

            string name = dto.Name.Trim();
            bool nameExists = await _context.Labels.AnyAsync(l =>l.Id != labelId && l.Name == name);

            if (nameExists)
            {
                throw new BusinessConflictException("Tên nhãn đã tồn tại.");
            }
            label.Name = name;
            label.Color = dto.Color.Trim();
            await SaveWorkspaceLabelChangesAsync();
            var response = MapWorkspaceLabel(label);
            return response;
        }

        public async Task DeleteWorkspaceLabelAsync(int workspaceId, int labelId, int userId)
        {
            await RequireWorkspaceAccessAsync(workspaceId, userId, true);
            var label = await _context.Labels.Include(l => l.Tasks).FirstOrDefaultAsync(l => l.Id == labelId && l.WorkspaceId == workspaceId);
            if (label == null)
            {
                throw new KeyNotFoundException("Không tìm thấy nhãn trong workspace.");
            }
            if (label.Tasks.Count > 0)
            {
                throw new BusinessConflictException(
                    "Hãy gỡ nhãn khỏi các task trước khi xóa nhãn.");
            }
            _context.Labels.Remove(label);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                var sqlException = ex.GetBaseException() as Microsoft.Data.SqlClient.SqlException;
                if (sqlException != null && sqlException.Number == 547)
                {
                    throw new BusinessConflictException("Nhãn đang được sử dụng nên không thể xóa.");
                }
                throw;
            }
        }

    }
}
